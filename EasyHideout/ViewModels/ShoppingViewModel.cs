using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.ViewModels;

public class ShoppingItemRow
{
    public string TarkovItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string IconPath { get; init; } = "";
    public int QuantityNeeded { get; init; }
    public int AvgPrice { get; init; }
    public int PreviousAvgPrice { get; init; }
    public int MinLevelForFlea { get; init; }
    public bool IsFleaLocked { get; init; }
    public long TotalPrice => (long)QuantityNeeded * AvgPrice;
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
    public string UnitPriceText => AvgPrice == 0 ? "—" : $"~{AvgPrice:N0}₽";
    public string TotalPriceText => AvgPrice == 0 ? "—" : $"~{TotalPrice:N0}₽";
    public double PriceDrop => PreviousAvgPrice > 0 && AvgPrice > 0 && AvgPrice < PreviousAvgPrice
        ? (double)(PreviousAvgPrice - AvgPrice) / PreviousAvgPrice : 0;
    public bool HasPriceDrop => PriceDrop >= 0.15;
    public string PriceDropText => HasPriceDrop ? $"↓ {PriceDrop:0%}" : "";
}

public class ShoppingStationGroup
{
    public string StationName { get; init; } = "";
    public int StationLevel { get; init; }
    public double PriorityScore { get; init; }
    public List<ShoppingItemRow> Items { get; init; } = new();
    public string GroupLabel => $"{StationName} L{StationLevel}";
    public long TotalCost => Items.Sum(i => i.TotalPrice);
    public string TotalCostText => TotalCost == 0 ? "—" : $"~{TotalCost:N0}₽";
}

public class ShoppingViewModel : INotifyPropertyChanged
{
    private bool _showByStation;
    private bool _isEmpty = true;
    private string _emptyMessage = "";
    private long _grandTotal;
    private bool _isFleaLocked;

    public ObservableCollection<ShoppingItemRow> FlatItems { get; } = new();
    public ObservableCollection<ShoppingStationGroup> StationGroups { get; } = new();

    public bool ShowByStation
    {
        get => _showByStation;
        set { _showByStation = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowFlat)); }
    }
    public bool ShowFlat => !_showByStation;

    public bool IsEmpty
    {
        get => _isEmpty;
        private set { _isEmpty = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasItems)); }
    }
    public bool HasItems => !_isEmpty;

    public string EmptyMessage
    {
        get => _emptyMessage;
        private set { _emptyMessage = value; OnPropertyChanged(); }
    }

    public long GrandTotal
    {
        get => _grandTotal;
        private set { _grandTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(GrandTotalText)); }
    }
    public string GrandTotalText => GrandTotal == 0 ? "—" : $"~{GrandTotal:N0}₽";

    public bool IsFleaLocked
    {
        get => _isFleaLocked;
        private set { _isFleaLocked = value; OnPropertyChanged(); }
    }

    public RelayCommand ShowListCommand { get; }
    public RelayCommand ShowByStationCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public ShoppingViewModel()
    {
        ShowListCommand = new RelayCommand(() => ShowByStation = false);
        ShowByStationCommand = new RelayCommand(() => ShowByStation = true);
        RefreshCommand = new RelayCommand(Load);
        AppEvents.NameFormatChanged += Load;
        AppEvents.DataRefreshed += Load;
    }

    public void Load()
    {
        FlatItems.Clear();
        StationGroups.Clear();

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null)
        {
            EmptyMessage = "No active profile selected.";
            IsEmpty = true;
            return;
        }

        var profileId = settings.ActiveProfileId.Value;
        var characterLevel = db.Profiles.Find(profileId)?.CharacterLevel ?? 1;

        if (characterLevel < 15)
        {
            IsFleaLocked = true;
            EmptyMessage = $"Flea Market unlocks at PMC level 15 (you are level {characterLevel}).";
            IsEmpty = true;
            return;
        }
        IsFleaLocked = false;

        var stations = db.HideoutStations
            .Include(s => s.Levels).ThenInclude(l => l.ItemRequirements)
            .Include(s => s.Levels).ThenInclude(l => l.StationDependencies).ThenInclude(d => d.RequiredStation)
            .Include(s => s.Levels).ThenInclude(l => l.TraderRequirements)
            .ToList();

        var profileLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

        var traderLookup = db.TraderLoyaltyLevels
            .ToDictionary(t => (t.TraderId, t.LoyaltyLevel), t => t.RequiredPlayerLevel);

        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        var priceSnapshots = db.ItemPriceSnapshots
            .ToDictionary(x => x.TarkovItemId, x => x.PreviousAvgPrice);



        // Compute priority scores for station grouping (same as Priority tab)
        var chainValues = new Dictionary<int, int>();
        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            if (cur >= station.MaxLevel && station.MaxLevel > 0) continue;
            var next = station.Levels.FirstOrDefault(l => l.Level == cur + 1);
            if (next == null || BlockingHelper.IsBlocked(next, profileLevels, characterLevel, traderLookup)) continue;
            var nonCurr = next.ItemRequirements.Where(r => !CurrencyFilter.IsCurrency(r.TarkovItemId)).ToList();
            var pct = ProgressHelper.NaiveCompletion(nonCurr, itemCounts);
            var chain = chainValues.TryGetValue(station.Id, out var cv) ? cv : 0;
            chainValues[station.Id] = (int)(pct * 100 * (1.0 + chain * 0.15));
        }

        // Flat aggregation: non-FiR items only, one row per item
        var flatAgg = new Dictionary<string, (string Name, string Short, string IconUrl, int Total, int Price, int FleaLevel)>();

        // Station groups: non-FiR items grouped by station
        var stationData = new List<(string Name, int Level, double Score, List<(string Id, string ItemName, string Short, string IconUrl, int Qty, int Price, int FleaLevel)> Items)>();

        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            var nextLvl = station.Levels.FirstOrDefault(l => l.Level == cur + 1);
            if (nextLvl == null) continue;
            if (BlockingHelper.IsBlocked(nextLvl, profileLevels, characterLevel, traderLookup)) continue;

            var nonCurr = nextLvl.ItemRequirements
                .Where(r => !CurrencyFilter.IsCurrency(r.TarkovItemId) && !r.FoundInRaid)
                .ToList();
            if (nonCurr.Count == 0) continue;

            var stationItems = new List<(string, string, string, string, int, int, int)>();
            foreach (var req in nonCurr)
            {
                var owned = itemCounts.TryGetValue(req.TarkovItemId, out var q) ? q : 0;
                var remaining = Math.Max(0, req.Quantity - owned);
                if (remaining == 0) continue;

                stationItems.Add((req.TarkovItemId, req.ItemName, req.ShortName, req.IconUrl, remaining, req.AvgPrice, req.MinLevelForFlea));

                if (flatAgg.TryGetValue(req.TarkovItemId, out var ex))
                    flatAgg[req.TarkovItemId] = (ex.Name, ex.Short, ex.IconUrl, ex.Total + remaining,
                        ex.Price == 0 ? req.AvgPrice : ex.Price, ex.FleaLevel == 0 ? req.MinLevelForFlea : ex.FleaLevel);
                else
                    flatAgg[req.TarkovItemId] = (req.ItemName, req.ShortName, req.IconUrl, remaining, req.AvgPrice, req.MinLevelForFlea);
            }

            if (stationItems.Count > 0)
            {
                var score = chainValues.TryGetValue(station.Id, out var sc) ? sc : 0;
                stationData.Add((station.Name, nextLvl.Level, score, stationItems));
            }
        }

        if (flatAgg.Count == 0)
        {
            EmptyMessage = stations.Count == 0
                ? "No game data — pull hideout data in Settings first."
                : "No buyable items needed for upcoming upgrades.";
            IsEmpty = true;
            GrandTotal = 0;
            return;
        }

        // Build flat list sorted by total cost descending
        foreach (var kvp in flatAgg.OrderByDescending(x => x.Value.Total * x.Value.Price))
        {
            var iconPath = AppMode.ResolveIconPath(kvp.Value.IconUrl);

            FlatItems.Add(new ShoppingItemRow
            {
                TarkovItemId = kvp.Key,
                ItemName = kvp.Value.Name,
                ShortName = kvp.Value.Short,
                IconPath = iconPath,
                QuantityNeeded = kvp.Value.Total,
                AvgPrice = kvp.Value.Price,
                PreviousAvgPrice = priceSnapshots.TryGetValue(kvp.Key, out var prev) ? prev : 0,
                MinLevelForFlea = kvp.Value.FleaLevel,
                IsFleaLocked = kvp.Value.FleaLevel > characterLevel,
            });
        }

        // Build station groups sorted by priority score descending
        foreach (var (name, level, score, items) in stationData.OrderByDescending(x => x.Score))
        {
            var group = new ShoppingStationGroup
            {
                StationName = name,
                StationLevel = level,
                PriorityScore = score,
                Items = items.Select(i =>
                {
                    return new ShoppingItemRow
                    {
                        TarkovItemId = i.Id,
                        ItemName = i.ItemName,
                        ShortName = i.Short,
                        IconPath = AppMode.ResolveIconPath(i.IconUrl),
                        QuantityNeeded = i.Qty,
                        AvgPrice = i.Price,
                        PreviousAvgPrice = priceSnapshots.TryGetValue(i.Id, out var prev2) ? prev2 : 0,
                        MinLevelForFlea = i.FleaLevel,
                        IsFleaLocked = i.FleaLevel > characterLevel,
                    };
                }).ToList(),
            };
            StationGroups.Add(group);
        }

        GrandTotal = FlatItems.Sum(i => i.TotalPrice);  // long sum, no overflow
        IsEmpty = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
