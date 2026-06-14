using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.ViewModels;

public record StationOption(int Id, string Name, int MaxLevel);

public class BuildStep
{
    public string StationName { get; init; } = "";
    public int Level { get; init; }
    public bool IsComplete { get; init; }
    public bool IsIncomplete => !IsComplete;
    public string Label => $"{StationName} L{Level}";
}

public class FarmItemRow
{
    public string IconPath { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public int Remaining { get; init; }
    public string StepLabel { get; init; } = "";
    public bool FoundInRaid { get; init; }
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
}

public class FocusStationViewModel : INotifyPropertyChanged
{
    private StationOption? _selectedStation;
    private int _selectedLevel;
    private bool _hasSelection;
    private bool _isEmpty = true;
    private string _emptyMessage = "";
    private string _pathSummary = "";
    private string _itemSummary = "";

    public ObservableCollection<StationOption> StationOptions { get; } = new();
    public ObservableCollection<int> LevelOptions { get; } = new();
    public ObservableCollection<BuildStep> BuildPath { get; } = new();
    public ObservableCollection<FarmItemRow> FarmingItems { get; } = new();

    public StationOption? SelectedStation
    {
        get => _selectedStation;
        set
        {
            if (ReferenceEquals(_selectedStation, value)) return;
            _selectedStation = value;
            OnPropertyChanged();
            RebuildLevelOptions();
            var newLevel = LevelOptions.Contains(_selectedLevel) ? _selectedLevel
                         : LevelOptions.Count > 0 ? LevelOptions[0] : 0;
            _selectedLevel = newLevel;
            OnPropertyChanged(nameof(SelectedLevel));
            Recalculate();
            SaveSelection();
        }
    }

    public int SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (_selectedLevel == value) return;
            _selectedLevel = value;
            OnPropertyChanged();
            Recalculate();
            SaveSelection();
        }
    }

    public bool HasSelection
    {
        get => _hasSelection;
        private set { _hasSelection = value; OnPropertyChanged(); }
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set { _isEmpty = value; OnPropertyChanged(); }
    }

    public string EmptyMessage
    {
        get => _emptyMessage;
        private set { _emptyMessage = value; OnPropertyChanged(); }
    }

    public string PathSummary
    {
        get => _pathSummary;
        private set { _pathSummary = value; OnPropertyChanged(); }
    }

    public string ItemSummary
    {
        get => _itemSummary;
        private set { _itemSummary = value; OnPropertyChanged(); }
    }

    public FocusStationViewModel()
    {
        AppEvents.DataRefreshed += Load;
        AppEvents.NameFormatChanged += Load;
    }

    public void Load()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null)
        {
            EmptyMessage = "No active profile selected.";
            IsEmpty = true;
            HasSelection = false;
            return;
        }

        var profileId = settings.ActiveProfileId.Value;
        var stations = db.HideoutStations.OrderBy(s => s.Name).ToList();

        if (stations.Count == 0)
        {
            EmptyMessage = "No game data — pull hideout data in Settings first.";
            IsEmpty = true;
            HasSelection = false;
            return;
        }

        var savedFocus = db.FocusNodes.FirstOrDefault(f => f.ProfileId == profileId);

        StationOptions.Clear();
        foreach (var s in stations)
            StationOptions.Add(new StationOption(s.Id, s.Name, s.MaxLevel));

        IsEmpty = false;
        EmptyMessage = "";

        // Restore saved selection without triggering saves
        var targetStation = savedFocus != null
            ? StationOptions.FirstOrDefault(o => o.Id == savedFocus.StationId)
            : null;
        targetStation ??= StationOptions.FirstOrDefault();

        _selectedStation = targetStation;
        OnPropertyChanged(nameof(SelectedStation));
        RebuildLevelOptions();

        var savedLevel = savedFocus?.TargetLevel ?? 0;
        _selectedLevel = LevelOptions.Contains(savedLevel) ? savedLevel
                       : LevelOptions.Count > 0 ? LevelOptions[0] : 0;
        OnPropertyChanged(nameof(SelectedLevel));

        Recalculate();
    }

    private void RebuildLevelOptions()
    {
        LevelOptions.Clear();
        if (_selectedStation == null) return;
        for (int i = 1; i <= _selectedStation.MaxLevel; i++)
            LevelOptions.Add(i);
    }

    private void Recalculate()
    {
        BuildPath.Clear();
        FarmingItems.Clear();
        HasSelection = false;

        if (_selectedStation == null || _selectedLevel == 0) return;

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        var stations = db.HideoutStations
            .Include(s => s.Levels).ThenInclude(l => l.StationDependencies)
            .Include(s => s.Levels).ThenInclude(l => l.ItemRequirements)
            .ToDictionary(s => s.Id);

        var profileLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        // Get full ordered chain (all steps including already-built)
        var fullChain = GetChain(_selectedStation.Id, _selectedLevel, stations);

        // Build path panel shows all steps, greyed if complete
        foreach (var step in fullChain)
        {
            var cur = profileLevels.GetValueOrDefault(step.StationId, 0);
            BuildPath.Add(new BuildStep
            {
                StationName = step.StationName,
                Level = step.Level,
                IsComplete = cur >= step.Level,
            });
        }

        var completeCount = BuildPath.Count(s => s.IsComplete);
        PathSummary = fullChain.Count == 0 ? "" : $"{completeCount} / {fullChain.Count} done";

        // Farming list: only incomplete steps, in build order
        var incompleteChain = fullChain
            .Where(s => profileLevels.GetValueOrDefault(s.StationId, 0) < s.Level)
            .ToList();

        foreach (var step in incompleteChain)
        {
            if (!stations.TryGetValue(step.StationId, out var station)) continue;
            var levelData = station.Levels.FirstOrDefault(l => l.Level == step.Level);
            if (levelData == null) continue;

            var stepLabel = $"{step.StationName} L{step.Level}";
            foreach (var req in levelData.ItemRequirements)
            {
                if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                var owned = itemCounts.TryGetValue(req.TarkovItemId, out var q) ? q : 0;
                var remaining = Math.Max(0, req.Quantity - owned);
                if (remaining == 0) continue;

                FarmingItems.Add(new FarmItemRow
                {
                    IconPath = AppMode.ResolveIconPath(req.IconUrl),
                    ItemName = req.ItemName,
                    ShortName = req.ShortName,
                    Remaining = remaining,
                    StepLabel = stepLabel,
                    FoundInRaid = req.FoundInRaid,
                });
            }
        }

        if (incompleteChain.Count == 0)
            ItemSummary = "All steps already complete!";
        else if (FarmingItems.Count == 0)
            ItemSummary = "All items collected — ready to build.";
        else
            ItemSummary = $"{FarmingItems.Count} items needed";

        HasSelection = true;
    }

    private static List<(int StationId, string StationName, int Level)> GetChain(
        int stationId, int level, Dictionary<int, HideoutStation> stations)
    {
        var visited = new HashSet<(int, int)>();
        var result = new List<(int StationId, string StationName, int Level)>();
        AddToChain(stationId, level, stations, visited, result);
        return result;
    }

    private static void AddToChain(
        int stationId, int level,
        Dictionary<int, HideoutStation> stations,
        HashSet<(int, int)> visited,
        List<(int StationId, string StationName, int Level)> result)
    {
        var key = (stationId, level);
        if (visited.Contains(key)) return;
        visited.Add(key);

        if (!stations.TryGetValue(stationId, out var station)) return;
        var levelData = station.Levels.FirstOrDefault(l => l.Level == level);
        if (levelData == null) return;

        // Previous levels of this station must be built first
        if (level > 1)
            AddToChain(stationId, level - 1, stations, visited, result);

        // Cross-station dependencies
        foreach (var dep in levelData.StationDependencies)
            AddToChain(dep.RequiredStationId, dep.RequiredLevel, stations, visited, result);

        result.Add((stationId, station.Name, level));
    }

    private void SaveSelection()
    {
        if (_selectedStation == null || _selectedLevel == 0) return;
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings?.ActiveProfileId == null) return;
            var profileId = settings.ActiveProfileId.Value;

            var existing = db.FocusNodes.FirstOrDefault(f => f.ProfileId == profileId);
            if (existing != null)
            {
                existing.StationId = _selectedStation.Id;
                existing.TargetLevel = _selectedLevel;
            }
            else
            {
                db.FocusNodes.Add(new FocusNode
                {
                    ProfileId = profileId,
                    StationId = _selectedStation.Id,
                    TargetLevel = _selectedLevel,
                });
            }
            db.SaveChanges();
        }
        catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
