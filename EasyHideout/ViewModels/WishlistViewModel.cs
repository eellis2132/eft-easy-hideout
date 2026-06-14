using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyHideout.Data;
using EasyHideout.Helpers;
using Microsoft.EntityFrameworkCore;
using EasyHideout.Models;
using EasyHideout.Services;

namespace EasyHideout.ViewModels;

public class WishlistRow : INotifyPropertyChanged
{
    private int _quantityOwned;
    private Action<WishlistRow>? _onChanged;
    private RelayCommand? _incrementCommand;
    private RelayCommand? _decrementCommand;

    public string TarkovItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string IconPath { get; init; } = "";
    public int TotalNeeded { get; init; }
    public bool FoundInRaid { get; init; }
    public int AvgPrice { get; init; }
    public int PreviousAvgPrice { get; init; }
    public int MinLevelForFlea { get; init; }
    public string StationTooltip { get; init; } = "";
    public bool IsFocusItem { get; init; }
    public string FocusTooltip { get; init; } = "";
    public double PriceDrop => !FoundInRaid && PreviousAvgPrice > 0 && AvgPrice > 0 && AvgPrice < PreviousAvgPrice
        ? (double)(PreviousAvgPrice - AvgPrice) / PreviousAvgPrice : 0;
    public bool HasPriceDrop => PriceDrop >= 0.15;
    public string PriceDropText => HasPriceDrop ? $"↓ {PriceDrop:0%}" : "";

    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
    public bool IsFullyStocked => QuantityOwned >= TotalNeeded;
    public double GatheringPercent =>
        TotalNeeded == 0 ? 1.0 : Math.Min(1.0, (double)_quantityOwned / TotalNeeded);

    public RelayCommand IncrementCommand => _incrementCommand ??= new RelayCommand(() => QuantityOwned++);
    public RelayCommand DecrementCommand => _decrementCommand ??= new RelayCommand(() => QuantityOwned--);

    public int QuantityOwned
    {
        get => _quantityOwned;
        set
        {
            var clamped = Math.Max(0, value);
            if (_quantityOwned == clamped) return;
            _quantityOwned = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFullyStocked));
            OnPropertyChanged(nameof(GatheringPercent));
            _onChanged?.Invoke(this);
        }
    }

    public void SetOnChanged(Action<WishlistRow> action) => _onChanged = action;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class WishlistViewModel : INotifyPropertyChanged
{
    private readonly List<WishlistRow> _allRows = new();
    private string _summary = "";
    private bool _isEmpty = true;
    private string _searchText = "";
    private double _gatheringPercent;
    private string _gatheringLabel = "";

    public ObservableCollection<WishlistRow> Items { get; } = new();

    public string Summary
    {
        get => _summary;
        private set { _summary = value; OnPropertyChanged(); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set { _isEmpty = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasItems)); }
    }

    public bool HasItems => !_isEmpty;

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); ApplyFilterAndSort(); }
    }

    public double GatheringPercent
    {
        get => _gatheringPercent;
        private set { _gatheringPercent = value; OnPropertyChanged(); }
    }

    public string GatheringLabel
    {
        get => _gatheringLabel;
        private set { _gatheringLabel = value; OnPropertyChanged(); }
    }

    public RelayCommand RefreshCommand { get; }

    public WishlistViewModel()
    {
        RefreshCommand = new RelayCommand(Load);
        AppEvents.NameFormatChanged += Load;
        AppEvents.DataRefreshed += Load;
    }

    public void Load()
    {
        _allRows.Clear();
        Items.Clear();

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null)
        {
            Summary = "No active profile selected.";
            IsEmpty = true;
            return;
        }

        var profileId = settings.ActiveProfileId.Value;

        var stations = db.HideoutStations
            .Include(s => s.Levels)
                .ThenInclude(l => l.ItemRequirements)
            .Include(s => s.Levels)
                .ThenInclude(l => l.StationDependencies)
                    .ThenInclude(d => d.RequiredStation)
            .Include(s => s.Levels)
                .ThenInclude(l => l.TraderRequirements)
            .ToList();

        var profileLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

        var characterLevel = db.Profiles.Find(profileId)?.CharacterLevel ?? 1;
        var traderLookup = db.TraderLoyaltyLevels
            .ToDictionary(t => (t.TraderId, t.LoyaltyLevel), t => t.RequiredPlayerLevel);

        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);



        var aggregated = new Dictionary<string, (string Name, string ShortName, string IconUrl, int Total, List<string> Stations, bool FiR, int Price, int FleaLevel)>();
        var l1FocusDemand = new Dictionary<string, (int Stations, int Units, List<string> StationNames)>();

        foreach (var station in stations)
        {
            var currentLevel = profileLevels.TryGetValue(station.Id, out var lvl) ? lvl : 0;
            var nextLevel = station.Levels.FirstOrDefault(l => l.Level == currentLevel + 1);
            if (nextLevel == null) continue;

            if (BlockingHelper.IsBlocked(nextLevel, profileLevels, characterLevel, traderLookup)) continue;

            var stationLabel = $"{station.Name} L{nextLevel.Level}";

            foreach (var req in nextLevel.ItemRequirements)
            {
                if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;

                if (aggregated.TryGetValue(req.TarkovItemId, out var existing))
                {
                    existing.Stations.Add(stationLabel);
                    aggregated[req.TarkovItemId] = (existing.Name, existing.ShortName, existing.IconUrl,
                        existing.Total + req.Quantity, existing.Stations,
                        existing.FiR || req.FoundInRaid, existing.Price == 0 ? req.AvgPrice : existing.Price,
                        existing.FleaLevel == 0 ? req.MinLevelForFlea : existing.FleaLevel);
                }
                else
                    aggregated[req.TarkovItemId] = (req.ItemName, req.ShortName, req.IconUrl, req.Quantity,
                        new List<string> { stationLabel }, req.FoundInRaid, req.AvgPrice, req.MinLevelForFlea);

                // Per-station remaining for focus scoring (matches PriorityViewModel exactly)
                var ownedNow = itemCounts.TryGetValue(req.TarkovItemId, out var qOwned) ? qOwned : 0;
                var rem = Math.Max(0, req.Quantity - ownedNow);
                if (rem > 0)
                {
                    if (l1FocusDemand.TryGetValue(req.TarkovItemId, out var fd))
                        l1FocusDemand[req.TarkovItemId] = (fd.Stations + 1, fd.Units + rem, [..fd.StationNames, stationLabel]);
                    else
                        l1FocusDemand[req.TarkovItemId] = (1, rem, [stationLabel]);
                }
            }
        }

        if (aggregated.Count == 0)
        {
            Summary = stations.Count == 0
                ? "No game data — pull hideout data in Settings first."
                : "No upcoming upgrades require items right now.";
            IsEmpty = true;
            return;
        }

        // l1FocusDemand already built above with per-station remaining (matches PriorityViewModel exactly)

        var l2Score = new Dictionary<string, (int Stations, int Units)>();
        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            if (cur >= station.MaxLevel && station.MaxLevel > 0) continue;
            var l2Lvl = station.Levels.FirstOrDefault(l => l.Level == cur + 2);
            if (l2Lvl == null) continue;
            foreach (var req in l2Lvl.ItemRequirements)
            {
                if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                var owned2 = itemCounts.TryGetValue(req.TarkovItemId, out var q2) ? q2 : 0;
                var rem2 = Math.Max(0, req.Quantity - owned2);
                if (rem2 == 0) continue;
                if (l2Score.TryGetValue(req.TarkovItemId, out var ex2))
                    l2Score[req.TarkovItemId] = (ex2.Stations + 1, ex2.Units + rem2);
                else
                    l2Score[req.TarkovItemId] = (1, rem2);
            }
        }

        var focusData = l1FocusDemand
            .Select(kvp => { l2Score.TryGetValue(kvp.Key, out var d2); return (kvp.Key, sc: kvp.Value.Stations * kvp.Value.Units + 0.4 * d2.Stations * d2.Units, names: kvp.Value.StationNames); })
            .OrderByDescending(x => x.sc)
            .Take(10)
            .ToDictionary(x => x.Key, x => x.names);

        var focusIds = focusData.Keys.ToHashSet();

        var priceSnapshots = db.ItemPriceSnapshots
            .ToDictionary(x => x.TarkovItemId, x => x.PreviousAvgPrice);

        foreach (var kvp in aggregated)
        {
            var iconPath = AppMode.ResolveIconPath(kvp.Value.IconUrl);

            var stationList = kvp.Value.Stations;
            var stationPart = stationList.Count == 1
                ? $"Needed by: {stationList[0]}"
                : $"Needed by: {string.Join(", ", stationList)}";
            var pricePart = !kvp.Value.FiR && kvp.Value.Price > 0
                ? $"  ·  ~{kvp.Value.Price:N0}₽ ea · ~{(long)kvp.Value.Price * kvp.Value.Total:N0}₽ total"
                : "";
            var fleaPart = !kvp.Value.FiR && kvp.Value.FleaLevel > characterLevel
                ? $"  ·  flea locked (need PMC {kvp.Value.FleaLevel})"
                : "";

            var isFocus = focusIds.Contains(kvp.Key);
            var row = new WishlistRow
            {
                TarkovItemId = kvp.Key,
                ItemName = kvp.Value.Name,
                ShortName = kvp.Value.ShortName,
                TotalNeeded = kvp.Value.Total,
                IconPath = iconPath,
                FoundInRaid = kvp.Value.FiR,
                AvgPrice = kvp.Value.Price,
                PreviousAvgPrice = priceSnapshots.TryGetValue(kvp.Key, out var prev) ? prev : 0,
                MinLevelForFlea = kvp.Value.FleaLevel,
                StationTooltip = stationPart + pricePart + fleaPart,
                IsFocusItem = isFocus,
                FocusTooltip = isFocus ? "Needed by: " + string.Join(", ", focusData[kvp.Key]) : "",
                QuantityOwned = itemCounts.TryGetValue(kvp.Key, out var owned) ? owned : 0,
            };
            row.SetOnChanged(SaveItemCount);
            _allRows.Add(row);
        }

        IsEmpty = false;
        ApplyFilterAndSort();
        UpdateSummary();
    }

    private void ApplyFilterAndSort()
    {
        var q = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var filter = _searchText.Trim();
            q = q.Where(r =>
                r.ItemName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.ShortName.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        // Incomplete first, then alphabetical
        q = q.OrderBy(r => r.IsFullyStocked).ThenBy(r => r.ItemName);

        Items.Clear();
        foreach (var row in q)
            Items.Add(row);
    }

    private void SaveItemCount(WishlistRow row)
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings?.ActiveProfileId == null) return;
            var profileId = settings.ActiveProfileId.Value;

            var existing = db.ItemCounts.FirstOrDefault(x =>
                x.ProfileId == profileId && x.TarkovItemId == row.TarkovItemId);

            if (existing != null)
                existing.QuantityOwned = row.QuantityOwned;
            else
                db.ItemCounts.Add(new ItemCount
                {
                    ProfileId = profileId,
                    TarkovItemId = row.TarkovItemId,
                    ItemName = row.ItemName,
                    QuantityOwned = row.QuantityOwned,
                });

            db.SaveChanges();
            UpdateSummary();
        }
        catch { }
    }

    private void UpdateSummary()
    {
        var total = _allRows.Count;
        var stocked = _allRows.Count(r => r.IsFullyStocked);
        Summary = $"{stocked} / {total} items ready";
        GatheringPercent = total == 0 ? 0.0 : (double)stocked / total;
        GatheringLabel = total == 0 ? "" : $"{GatheringPercent:0%}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
