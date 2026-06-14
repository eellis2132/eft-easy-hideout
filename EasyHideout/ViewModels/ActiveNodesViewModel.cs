using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using EasyHideout.Data;
using EasyHideout.Helpers;
using Microsoft.EntityFrameworkCore;
using EasyHideout.Models;
using EasyHideout.Services;
using EasyHideout.Views;

namespace EasyHideout.ViewModels;

public record DependencyLink(int StationId, string DisplayText, bool IsNavigable = true);

public class StationTileViewModel : INotifyPropertyChanged
{
    private int _currentLevel;
    private bool _isBlocked;
    private double _completionPercent;
    private List<DependencyLink> _blockingDeps = new();

    public int StationId { get; init; }
    public string Name { get; init; } = "";
    public int MaxLevel { get; init; }

    public int CurrentLevel
    {
        get => _currentLevel;
        private set { _currentLevel = value; Notify(); Notify(nameof(IsMaxed)); Notify(nameof(LevelDisplay)); }
    }
    public bool IsBlocked { get => _isBlocked; private set { _isBlocked = value; Notify(); Notify(nameof(IsNotBuilt)); } }
    public bool IsMaxed => _currentLevel >= MaxLevel && MaxLevel > 0;
    public bool IsNotBuilt => _currentLevel == 0 && !IsMaxed;
    public double CompletionPercent { get => _completionPercent; private set { _completionPercent = value; Notify(); } }
    public IReadOnlyList<DependencyLink> BlockingDeps => _blockingDeps;
    public string LevelDisplay => IsMaxed ? "✓ Max" : _currentLevel == 0 ? "Not Started" : $"Lv {_currentLevel} / {MaxLevel}";

    public void Update(int currentLevel, bool isBlocked, double completionPercent, List<DependencyLink> blockingDeps)
    {
        _currentLevel = currentLevel;
        _isBlocked = isBlocked;
        _completionPercent = completionPercent;
        _blockingDeps = blockingDeps;
        Notify(nameof(CurrentLevel));
        Notify(nameof(IsBlocked));
        Notify(nameof(IsMaxed));
        Notify(nameof(IsNotBuilt));
        Notify(nameof(CompletionPercent));
        Notify(nameof(BlockingDeps));
        Notify(nameof(LevelDisplay));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class DetailItemRow
{
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string TarkovItemId { get; init; } = "";
    public string IconPath { get; init; } = "";
    public int Needed { get; init; }
    public int Owned { get; init; }
    public bool FoundInRaid { get; init; }
    public bool IsCurrency => CurrencyFilter.IsCurrency(TarkovItemId);
    public bool IsFullyStocked => Owned >= Needed;
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);
    public string QuantityText => IsCurrency
        ? Needed.ToString("N0")
        : $"{Owned.ToString("N0")} / {Needed.ToString("N0")}";
}

public class ActiveNodesViewModel : INotifyPropertyChanged
{
    private StationTileViewModel? _selected;
    private string _detailPos = "right";
    private int _selectedForceLevel;

    public ObservableCollection<StationTileViewModel> Stations { get; } = new();
    public ObservableCollection<DetailItemRow> L1Items { get; } = new();
    public ObservableCollection<DetailItemRow> L2Items { get; } = new();
    public ObservableCollection<DependencyLink> BlockingDeps { get; } = new();

    public List<int> ForceTargetLevels { get; private set; } = new();

    public int SelectedForceLevel
    {
        get => _selectedForceLevel;
        set { _selectedForceLevel = value; OnPropertyChanged(); }
    }

    public bool CanForce => _selected != null && ForceTargetLevels.Count > 0;

    public StationTileViewModel? SelectedStation
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanMarkUpgraded));
            OnPropertyChanged(nameof(CanDowngrade));
            OnPropertyChanged(nameof(SelectedName));
            OnPropertyChanged(nameof(SelectedLevelText));
            RebuildForceLevels();
            LoadDetailPanel();
        }
    }

    public bool HasSelection => _selected != null;
    public bool CanMarkUpgraded => _selected != null && !_selected.IsMaxed && !_selected.IsBlocked;
    public bool CanDowngrade => _selected != null && _selected.CurrentLevel > 0;
    public string SelectedName => _selected?.Name ?? "";
    public string SelectedLevelText => _selected == null ? "" : (_selected.IsMaxed ? $"Level {_selected.CurrentLevel} — Max" : $"Level {_selected.CurrentLevel}  →  {_selected.CurrentLevel + 1}");

    // Detail panel layout (driven by AppSettings.DetailPanelPosition)
    public Dock DetailDock => _detailPos switch
    {
        "left" => Dock.Left,
        "top" => Dock.Top,
        "bottom" => Dock.Bottom,
        _ => Dock.Right
    };
    public bool IsHorizontalDock => DetailDock == Dock.Top || DetailDock == Dock.Bottom;

    // Grid layout bindings — used by the view to position panel / splitter / content
    public int DetailRow    => DetailDock == Dock.Bottom ? 2 : 0;
    public int DetailColumn => DetailDock == Dock.Right  ? 2 : 0;
    public int DetailRowSpan => IsHorizontalDock ? 1 : 3;
    public int DetailColSpan => IsHorizontalDock ? 3 : 1;

    public int SplitterRow    => IsHorizontalDock ? 1 : 0;
    public int SplitterColumn => IsHorizontalDock ? 0 : 1;
    public int SplitterRowSpan => IsHorizontalDock ? 1 : 3;
    public int SplitterColSpan => IsHorizontalDock ? 3 : 1;
    public GridResizeDirection SplitterResizeDirection =>
        IsHorizontalDock ? GridResizeDirection.Rows : GridResizeDirection.Columns;

    public int ContentRow    => DetailDock == Dock.Top    ? 2 : 0;
    public int ContentColumn => DetailDock == Dock.Left   ? 2 : 0;
    public int ContentRowSpan => IsHorizontalDock ? 1 : 3;
    public int ContentColSpan => IsHorizontalDock ? 3 : 1;

    public RelayCommand MarkUpgradedCommand { get; }
    public RelayCommand DowngradeCommand { get; }
    public RelayCommand ForceCommand { get; }
    public RelayCommand<DependencyLink> NavigateToDependencyCommand { get; }

    public ActiveNodesViewModel()
    {
        MarkUpgradedCommand = new RelayCommand(DoMarkUpgraded, () => CanMarkUpgraded);
        DowngradeCommand = new RelayCommand(DoDowngrade, () => CanDowngrade);
        ForceCommand = new RelayCommand(DoForce, () => CanForce);
        NavigateToDependencyCommand = new RelayCommand<DependencyLink>(link =>
        {
            if (link == null) return;
            var target = Stations.FirstOrDefault(s => s.StationId == link.StationId);
            if (target != null) SelectedStation = target;
        });
        AppEvents.NameFormatChanged += () => { if (_selected != null) LoadDetailPanel(); };
    }

    public void Load()
    {
        Stations.Clear();
        SelectedStation = null;

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings == null) return;

        _detailPos = settings.DetailPanelPosition ?? "right";
        NotifyDetailLayout();

        if (settings.ActiveProfileId == null) return;
        PopulateStations(db, settings.ActiveProfileId.Value);
    }

    private void Refresh()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;

        if (Stations.Count == 0)
        {
            PopulateStations(db, settings.ActiveProfileId.Value);
            return;
        }

        UpdateStations(db, settings.ActiveProfileId.Value);
    }

    private void PopulateStations(AppDbContext db, int profileId)
    {
        var stations = LoadStations(db);
        var profileLevels = LoadProfileLevels(db, profileId);
        var completions = ServiceLocator.Get<DistributionService>().Compute(profileId);
        var characterLevel = db.Profiles.Find(profileId)?.CharacterLevel ?? 1;
        var traderLookup = db.TraderLoyaltyLevels
            .ToDictionary(t => (t.TraderId, t.LoyaltyLevel), t => t.RequiredPlayerLevel);

        foreach (var station in stations)
        {
            var tile = new StationTileViewModel { StationId = station.Id, Name = station.Name, MaxLevel = station.MaxLevel };
            ApplyState(tile, station, profileLevels, completions, characterLevel, traderLookup);
            Stations.Add(tile);
        }
    }

    private void UpdateStations(AppDbContext db, int profileId)
    {
        var stations = LoadStations(db).ToDictionary(s => s.Id);
        var profileLevels = LoadProfileLevels(db, profileId);
        var completions = ServiceLocator.Get<DistributionService>().Compute(profileId);
        var characterLevel = db.Profiles.Find(profileId)?.CharacterLevel ?? 1;
        var traderLookup = db.TraderLoyaltyLevels
            .ToDictionary(t => (t.TraderId, t.LoyaltyLevel), t => t.RequiredPlayerLevel);

        foreach (var tile in Stations)
        {
            if (!stations.TryGetValue(tile.StationId, out var station)) continue;
            ApplyState(tile, station, profileLevels, completions, characterLevel, traderLookup);
        }

        if (_selected != null)
        {
            OnPropertyChanged(nameof(CanMarkUpgraded));
            OnPropertyChanged(nameof(CanDowngrade));
            OnPropertyChanged(nameof(SelectedLevelText));
            LoadDetailPanel();
        }
    }

    private static List<Models.HideoutStation> LoadStations(AppDbContext db) =>
        db.HideoutStations
            .Include(s => s.Levels).ThenInclude(l => l.ItemRequirements)
            .Include(s => s.Levels).ThenInclude(l => l.StationDependencies).ThenInclude(d => d.RequiredStation)
            .Include(s => s.Levels).ThenInclude(l => l.TraderRequirements)
            .OrderBy(s => s.Name)
            .ToList();

    private static Dictionary<int, int> LoadProfileLevels(AppDbContext db, int profileId) =>
        db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

    private static void ApplyState(StationTileViewModel tile, Models.HideoutStation station,
        Dictionary<int, int> profileLevels, Dictionary<int, double> completions,
        int characterLevel, Dictionary<(string TraderId, int LL), int> traderLookup)
    {
        var currentLevel = profileLevels.TryGetValue(station.Id, out var lvl) ? lvl : 0;
        var isMaxed = currentLevel >= station.MaxLevel && station.MaxLevel > 0;
        var blockingDeps = new List<DependencyLink>();

        if (!isMaxed)
        {
            var nextLevel = station.Levels.FirstOrDefault(l => l.Level == currentLevel + 1);
            if (nextLevel != null)
            {
                foreach (var (stId, text, navigable) in BlockingHelper.GetBlockReasons(nextLevel, profileLevels, characterLevel, traderLookup))
                    blockingDeps.Add(new DependencyLink(stId, text, navigable));
            }
        }

        var completion = completions.TryGetValue(station.Id, out var pct) ? pct : (isMaxed ? 1.0 : 0.0);
        tile.Update(currentLevel, blockingDeps.Count > 0, completion, blockingDeps);
    }

    private void NotifyDetailLayout()
    {
        OnPropertyChanged(nameof(DetailDock));
        OnPropertyChanged(nameof(IsHorizontalDock));
        OnPropertyChanged(nameof(DetailRow));
        OnPropertyChanged(nameof(DetailColumn));
        OnPropertyChanged(nameof(DetailRowSpan));
        OnPropertyChanged(nameof(DetailColSpan));
        OnPropertyChanged(nameof(SplitterRow));
        OnPropertyChanged(nameof(SplitterColumn));
        OnPropertyChanged(nameof(SplitterRowSpan));
        OnPropertyChanged(nameof(SplitterColSpan));
        OnPropertyChanged(nameof(SplitterResizeDirection));
        OnPropertyChanged(nameof(ContentRow));
        OnPropertyChanged(nameof(ContentColumn));
        OnPropertyChanged(nameof(ContentRowSpan));
        OnPropertyChanged(nameof(ContentColSpan));
    }

    private void LoadDetailPanel()
    {
        L1Items.Clear();
        L2Items.Clear();
        BlockingDeps.Clear();

        if (_selected == null) return;

        foreach (var link in _selected.BlockingDeps)
            BlockingDeps.Add(link);

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        var profileLevels = LoadProfileLevels(db, profileId);
        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        var station = db.HideoutStations
            .Include(s => s.Levels).ThenInclude(l => l.ItemRequirements)
            .FirstOrDefault(s => s.Id == _selected.StationId);
        if (station == null) return;

        var currentLevel = profileLevels.TryGetValue(station.Id, out var cl) ? cl : 0;

        for (var tier = 1; tier <= 2; tier++)
        {
            var levelRecord = station.Levels.FirstOrDefault(l => l.Level == currentLevel + tier);
            if (levelRecord == null) continue;

            var target = tier == 1 ? L1Items : L2Items;
            foreach (var req in levelRecord.ItemRequirements.OrderBy(r => r.ItemName))
            {
                var iconPath = AppMode.ResolveIconPath(req.IconUrl);
                var owned = itemCounts.TryGetValue(req.TarkovItemId, out var qty) ? qty : 0;
                target.Add(new DetailItemRow
                {
                    TarkovItemId = req.TarkovItemId,
                    ItemName = req.ItemName,
                    ShortName = req.ShortName,
                    IconPath = iconPath,
                    Needed = req.Quantity,
                    Owned = owned,
                    FoundInRaid = req.FoundInRaid,
                });
            }
        }
    }

    private void DoMarkUpgraded()
    {
        if (_selected == null || !CanMarkUpgraded) return;

        var win = System.Windows.Application.Current.MainWindow;
        if (!ConfirmationDialog.Show(
            $"Mark '{_selected.Name}' as upgraded to Level {_selected.CurrentLevel + 1}?\n\nRequired items will be subtracted from your Item Pool.", win))
            return;

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        var profileLevels = LoadProfileLevels(db, profileId);
        var currentLevel = profileLevels.TryGetValue(_selected.StationId, out var cl) ? cl : 0;
        var nextLevel = currentLevel + 1;

        // Subtract item requirements for the level being completed
        var levelRecord = db.HideoutLevels
            .Include(l => l.ItemRequirements)
            .FirstOrDefault(l => l.StationId == _selected.StationId && l.Level == nextLevel);

        if (levelRecord != null)
        {
            foreach (var req in levelRecord.ItemRequirements)
            {
                var ic = db.ItemCounts.FirstOrDefault(x => x.ProfileId == profileId && x.TarkovItemId == req.TarkovItemId);
                if (ic != null)
                    ic.QuantityOwned = Math.Max(0, ic.QuantityOwned - req.Quantity);
            }
        }

        // Increment station level
        var psl = db.ProfileStationLevels.FirstOrDefault(x => x.ProfileId == profileId && x.StationId == _selected.StationId);
        if (psl != null)
            psl.CurrentLevel = nextLevel;
        else
            db.ProfileStationLevels.Add(new ProfileStationLevel { ProfileId = profileId, StationId = _selected.StationId, CurrentLevel = nextLevel });

        db.SaveChanges();
        UpdateStations(db, profileId);
    }

    private void RebuildForceLevels()
    {
        ForceTargetLevels = _selected == null
            ? new List<int>()
            : Enumerable.Range(0, _selected.MaxLevel + 1).ToList();
        SelectedForceLevel = _selected?.CurrentLevel ?? 0;
        OnPropertyChanged(nameof(ForceTargetLevels));
        OnPropertyChanged(nameof(CanForce));
        ForceCommand.RaiseCanExecuteChanged();
    }

    private void DoForce()
    {
        if (_selected == null) return;

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        var allStations = LoadStations(db).ToDictionary(s => s.Id);
        var profileLevels = LoadProfileLevels(db, profileId);
        var targetLevel = SelectedForceLevel;

        var changes = ComputeRequiredLevels(_selected.StationId, targetLevel, allStations, profileLevels);

        if (changes.Count == 0)
            return; // already at this level, nothing to do

        // Build confirm message
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Force '{_selected.Name}' to Level {targetLevel}?");
        sb.AppendLine("Your Item Pool quantities will not be deducted.");

        var prereqs = changes.Where(c => c.Key != _selected.StationId).ToList();
        if (prereqs.Count > 0)
        {
            sb.AppendLine("\nWill also update:");
            foreach (var (sid, lvl) in prereqs.OrderBy(c => allStations.TryGetValue(c.Key, out var s) ? s.Name : ""))
            {
                if (allStations.TryGetValue(sid, out var st))
                    sb.AppendLine($"  • {st.Name} → Level {lvl}");
            }
        }

        var win = System.Windows.Application.Current.MainWindow;
        if (!ConfirmationDialog.Show(sb.ToString().TrimEnd(), win)) return;

        foreach (var (sid, newLevel) in changes)
        {
            var psl = db.ProfileStationLevels.FirstOrDefault(x => x.ProfileId == profileId && x.StationId == sid);
            if (psl != null)
                psl.CurrentLevel = newLevel;
            else
                db.ProfileStationLevels.Add(new ProfileStationLevel { ProfileId = profileId, StationId = sid, CurrentLevel = newLevel });
        }

        db.SaveChanges();
        UpdateStations(db, profileId);
    }

    // Iteratively expand required levels until stable — handles multiple paths requiring the same dep.
    private static Dictionary<int, int> ComputeRequiredLevels(
        int rootStationId, int rootTargetLevel,
        Dictionary<int, Models.HideoutStation> allStations,
        Dictionary<int, int> profileLevels)
    {
        // Seed with only stations that actually need changing
        var required = new Dictionary<int, int>();
        var rootCurrent = profileLevels.TryGetValue(rootStationId, out var rc) ? rc : 0;
        if (rootTargetLevel != rootCurrent)
            required[rootStationId] = rootTargetLevel;

        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var (sid, targetLvl) in required.ToList())
            {
                if (!allStations.TryGetValue(sid, out var station)) continue;

                for (int lvl = 1; lvl <= targetLvl; lvl++)
                {
                    var levelRecord = station.Levels.FirstOrDefault(l => l.Level == lvl);
                    if (levelRecord == null) continue;

                    foreach (var dep in levelRecord.StationDependencies)
                    {
                        var effectiveCurrent = Math.Max(
                            profileLevels.TryGetValue(dep.RequiredStationId, out var pl) ? pl : 0,
                            required.TryGetValue(dep.RequiredStationId, out var rl) ? rl : 0);

                        if (dep.RequiredLevel > effectiveCurrent)
                        {
                            required[dep.RequiredStationId] = Math.Max(
                                dep.RequiredLevel,
                                required.TryGetValue(dep.RequiredStationId, out var ex) ? ex : 0);
                            changed = true;
                        }
                    }
                }
            }
        }

        return required;
    }

    private void DoDowngrade()
    {
        if (_selected == null || !CanDowngrade) return;

        var win = System.Windows.Application.Current.MainWindow;
        if (!ConfirmationDialog.Show(
            $"Downgrade '{_selected.Name}' back to Level {_selected.CurrentLevel - 1}?\n\nYour Item Pool quantities will not be restored.", win))
            return;

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null) return;
        var profileId = settings.ActiveProfileId.Value;

        var profileLevels = LoadProfileLevels(db, profileId);
        var currentLevel = profileLevels.TryGetValue(_selected.StationId, out var cl) ? cl : 0;

        var psl = db.ProfileStationLevels.FirstOrDefault(x => x.ProfileId == profileId && x.StationId == _selected.StationId);
        if (psl != null)
        {
            psl.CurrentLevel = Math.Max(0, currentLevel - 1);
            db.SaveChanges();
        }

        UpdateStations(db, profileId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
