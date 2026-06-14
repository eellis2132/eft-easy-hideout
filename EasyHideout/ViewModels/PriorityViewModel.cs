using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.ViewModels;

public class FocusItemRow
{
    public string TarkovItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string IconPath { get; init; } = "";
    public int L1StationCount { get; init; }
    public int TotalRemaining { get; init; }
    public bool FoundInRaid { get; init; }
    public string StationsTooltip { get; init; } = "";
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
    public string StationLabel => L1StationCount == 1 ? "× 1 station" : $"× {L1StationCount} stations";
}

public class PriorityItemRow
{
    public int Rank { get; init; }
    public string NodeTag { get; init; } = "";
    public string StationName { get; init; } = "";
    public int StationId { get; init; }
    public double CompletionPercent { get; init; }
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string TarkovItemId { get; init; } = "";
    public int QuantityOwned { get; init; }
    public int QuantityNeeded { get; init; }
    public int QuantityRemaining { get; init; }
    public bool IsOneAway { get; init; }
    public bool FoundInRaid { get; init; }
    public int AvgPrice { get; init; }
    public int MinLevelForFlea { get; init; }
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
}

public class LimitOption
{
    public int Value { get; init; }
    public string Label { get; init; } = "";
}

public class PriorityViewModel : INotifyPropertyChanged
{
    // Focus Items: auto-ranked by cross-station impact (replaces manual Important Items)
    public ObservableCollection<FocusItemRow> FocusItems { get; } = new();

    // L+1 split into one-away (always shown in full) and regular (limited by Show dropdown)
    public ObservableCollection<PriorityItemRow> L1OneAwayItems { get; } = new();
    public ObservableCollection<PriorityItemRow> L1RegularItems { get; } = new();

    public List<LimitOption> LimitOptions { get; } = new()
    {
        new() { Value = 5,  Label = "5"   },
        new() { Value = 10, Label = "10"  },
        new() { Value = 15, Label = "15"  },
        new() { Value = 20, Label = "20"  },
        new() { Value = 25, Label = "25"  },
        new() { Value = 50, Label = "50"  },
        new() { Value = 0,  Label = "All" },
    };

    private LimitOption _l1Limit;
    public LimitOption L1Limit
    {
        get => _l1Limit;
        set { _l1Limit = value; OnPropertyChanged(); ApplyLimits(); SaveLimits(); }
    }

    private readonly List<PriorityItemRow> _fullOneAwayRows = new();
    private readonly List<PriorityItemRow> _fullRegularRows = new();

    public PriorityViewModel()
    {
        _l1Limit = LimitOptions[2]; // 15 default
        AppEvents.NameFormatChanged += Load;
        AppEvents.DataRefreshed += Load;
    }

    private void ApplyLimits()
    {
        L1OneAwayItems.Clear();
        foreach (var r in _fullOneAwayRows) L1OneAwayItems.Add(r);

        L1RegularItems.Clear();
        var source = _l1Limit.Value == 0 ? _fullRegularRows : _fullRegularRows.Take(_l1Limit.Value);
        foreach (var r in source) L1RegularItems.Add(r);
    }

    public void Load()
    {
        _fullOneAwayRows.Clear();
        _fullRegularRows.Clear();
        FocusItems.Clear();
        L1OneAwayItems.Clear();
        L1RegularItems.Clear();

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        _l1Limit = LimitOptions.FirstOrDefault(o => o.Value == settings.PriorityL1Show) ?? LimitOptions[2];
        OnPropertyChanged(nameof(L1Limit));

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

        // ── Chain value: how many currently-blocked stations become unblocked if we upgrade ──
        var chainValues = new Dictionary<int, int>();
        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            if (cur >= station.MaxLevel && station.MaxLevel > 0) continue;
            chainValues[station.Id] = ComputeChainValue(station, cur + 1, stations, profileLevels, characterLevel, traderLookup);
        }

        // ── Focus Items: score items by how many active L+1 stations still need them ──
        var l1Demand = new Dictionary<string, (string Name, string Short, string IconUrl, int Stations, int Units, bool FiR, List<string> StationNames)>();
        var l2Demand = new Dictionary<string, (int Stations, int Units)>();

        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            if (cur >= station.MaxLevel && station.MaxLevel > 0) continue;

            var nextLvl = station.Levels.FirstOrDefault(l => l.Level == cur + 1);
            if (nextLvl == null || BlockingHelper.IsBlocked(nextLvl, profileLevels, characterLevel, traderLookup)) continue;

            var stationLabel = $"{station.Name} L{cur + 1}";

            foreach (var req in nextLvl.ItemRequirements)
            {
                if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                var owned = itemCounts.TryGetValue(req.TarkovItemId, out var q) ? q : 0;
                var rem = Math.Max(0, req.Quantity - owned);
                if (rem == 0) continue;

                if (l1Demand.TryGetValue(req.TarkovItemId, out var ex))
                    l1Demand[req.TarkovItemId] = (ex.Name, ex.Short, ex.IconUrl, ex.Stations + 1, ex.Units + rem, ex.FiR || req.FoundInRaid, [..ex.StationNames, stationLabel]);
                else
                    l1Demand[req.TarkovItemId] = (req.ItemName, req.ShortName, req.IconUrl, 1, rem, req.FoundInRaid, [stationLabel]);
            }

            // L+2 lookahead (weighted at 0.4)
            var l2Lvl = station.Levels.FirstOrDefault(l => l.Level == cur + 2);
            if (l2Lvl != null)
            {
                foreach (var req in l2Lvl.ItemRequirements)
                {
                    if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                    var owned = itemCounts.TryGetValue(req.TarkovItemId, out var q) ? q : 0;
                    var rem = Math.Max(0, req.Quantity - owned);
                    if (rem == 0) continue;

                    if (l2Demand.TryGetValue(req.TarkovItemId, out var ex))
                        l2Demand[req.TarkovItemId] = (ex.Stations + 1, ex.Units + rem);
                    else
                        l2Demand[req.TarkovItemId] = (1, rem);
                }
            }
        }

        foreach (var (id, data, score) in l1Demand
            .Select(kvp =>
            {
                l2Demand.TryGetValue(kvp.Key, out var d2);
                var sc = kvp.Value.Stations * kvp.Value.Units + 0.4 * d2.Stations * d2.Units;
                return (kvp.Key, kvp.Value, sc);
            })
            .OrderByDescending(x => x.sc)
            .Take(10))
        {
            var iconPath = AppMode.ResolveIconPath(data.IconUrl);
            FocusItems.Add(new FocusItemRow
            {
                TarkovItemId = id,
                ItemName = data.Name,
                ShortName = data.Short,
                IconPath = iconPath,
                L1StationCount = data.Stations,
                TotalRemaining = data.Units,
                FoundInRaid = data.FiR,
                StationsTooltip = "Needed by: " + string.Join(", ", data.StationNames),
            });
        }

        // ── L+1 readiness list ──
        var l1Rows = new List<(double Score, double NaivePct, int Rem, int Blockers,
            int StId, string StName, string ItemId, string ItemName, string ShortName, int Qty, int Owned,
            bool FiR, int Price, int FleaLevel)>();

        foreach (var station in stations)
        {
            var cur = profileLevels.TryGetValue(station.Id, out var lv) ? lv : 0;
            if (cur >= station.MaxLevel && station.MaxLevel > 0) continue;

            var nextLvl = station.Levels.FirstOrDefault(l => l.Level == cur + 1);
            if (nextLvl == null || BlockingHelper.IsBlocked(nextLvl, profileLevels, characterLevel, traderLookup)) continue;
            if (nextLvl.ItemRequirements.Count == 0) continue;

            var nonCurrency = nextLvl.ItemRequirements
                .Where(r => !CurrencyFilter.IsCurrency(r.TarkovItemId)).ToList();

            var naivePct = ProgressHelper.NaiveCompletion(nonCurrency, itemCounts);
            var chain = chainValues.TryGetValue(station.Id, out var cv) ? cv : 0;
            var stationScore = naivePct * (1.0 + chain * 0.15);

            int blockerCount = nonCurrency.Count(r =>
            {
                var o = itemCounts.TryGetValue(r.TarkovItemId, out var q) ? q : 0;
                return o < r.Quantity;
            });

            foreach (var req in nonCurrency)
            {
                var owned = itemCounts.TryGetValue(req.TarkovItemId, out var qty) ? qty : 0;
                var remaining = Math.Max(0, req.Quantity - owned);
                if (remaining == 0) continue;
                l1Rows.Add((stationScore, naivePct, remaining, blockerCount,
                    station.Id, station.Name, req.TarkovItemId, req.ItemName, req.ShortName, req.Quantity, owned,
                    req.FoundInRaid, req.AvgPrice, req.MinLevelForFlea));
            }
        }

        var sorted = l1Rows
            .OrderBy(x => x.Blockers == 1 ? 0 : 1)
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.Rem)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var t = sorted[i];
            var row = new PriorityItemRow
            {
                Rank = i + 1,
                NodeTag = MakeNodeTag(t.StName),
                StationName = t.StName,
                StationId = t.StId,
                CompletionPercent = t.NaivePct,
                ItemName = t.ItemName,
                ShortName = t.ShortName,
                TarkovItemId = t.ItemId,
                QuantityOwned = t.Owned,
                QuantityNeeded = t.Qty,
                QuantityRemaining = t.Rem,
                IsOneAway = t.Blockers == 1,
                FoundInRaid = t.FiR,
                AvgPrice = t.Price,
                MinLevelForFlea = t.FleaLevel,
            };

            if (t.Blockers == 1) _fullOneAwayRows.Add(row);
            else _fullRegularRows.Add(row);
        }

        ApplyLimits();
    }

    private void SaveLimits()
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null) return;
            settings.PriorityL1Show = _l1Limit.Value;
            db.SaveChanges();
        }
        catch { }
    }

    private static int ComputeChainValue(HideoutStation station, int newLevel,
        IEnumerable<HideoutStation> allStations, Dictionary<int, int> profileLevels,
        int characterLevel, Dictionary<(string TraderId, int LL), int> traderLookup)
    {
        int count = 0;
        var simulated = new Dictionary<int, int>(profileLevels) { [station.Id] = newLevel };

        foreach (var other in allStations)
        {
            if (other.Id == station.Id) continue;
            var otherCur = profileLevels.TryGetValue(other.Id, out var ol) ? ol : 0;
            if (otherCur >= other.MaxLevel && other.MaxLevel > 0) continue;

            var otherNext = other.Levels.FirstOrDefault(l => l.Level == otherCur + 1);
            if (otherNext == null) continue;

            if (BlockingHelper.IsBlocked(otherNext, profileLevels, characterLevel, traderLookup) &&
                !BlockingHelper.IsBlocked(otherNext, simulated, characterLevel, traderLookup))
                count++;
        }
        return count;
    }

    private static string MakeNodeTag(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
            return string.Concat(words.Select(w => char.ToUpper(w[0])));
        return name.Length <= 4 ? name.ToUpper() : name[..4].ToUpper();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
