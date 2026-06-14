using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.ViewModels;

public class ItemPoolRow : INotifyPropertyChanged
{
    private int _quantityOwned;
    private Action<ItemPoolRow>? _onChanged;
    private RelayCommand? _incrementCommand;
    private RelayCommand? _decrementCommand;

    public RelayCommand IncrementCommand => _incrementCommand ??= new RelayCommand(() => QuantityOwned++);
    public RelayCommand DecrementCommand => _decrementCommand ??= new RelayCommand(() => QuantityOwned--);

    public string TarkovItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string IconPath { get; init; } = "";
    public int TotalNeeded { get; init; }
    public bool FoundInRaid { get; init; }
    public string DisplayName => NameFormatHelper.Apply(ItemName, ShortName);

    public double GatheringPercent =>
        TotalNeeded == 0 ? 1.0 : Math.Min(1.0, (double)_quantityOwned / TotalNeeded);

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

    public bool IsFullyStocked => QuantityOwned >= TotalNeeded;

    public void SetOnChanged(Action<ItemPoolRow> action) => _onChanged = action;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class ItemPoolViewModel : INotifyPropertyChanged
{
    private readonly List<ItemPoolRow> _allRows = new();
    private string _summary = "";
    private bool _isEmpty = true;
    private string _searchText = "";
    private string _sortColumn = "Name";
    private bool _sortDescending = false;
    private double _gatheringPercent;
    private string _gatheringLabel = "";

    public ObservableCollection<ItemPoolRow> Items { get; } = new();

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

    public string SortIndicatorName     => SortIndicator("Name");
    public string SortIndicatorNeeded   => SortIndicator("Needed");
    public string SortIndicatorOwned    => SortIndicator("Owned");
    public string SortIndicatorProgress => SortIndicator("Progress");

    public RelayCommand RefreshCommand { get; }
    public RelayCommand<string> SortByCommand { get; }

    public ItemPoolViewModel()
    {
        RefreshCommand = new RelayCommand(Load);
        AppEvents.NameFormatChanged += Load;
        AppEvents.DataRefreshed += Load;
        SortByCommand = new RelayCommand<string>(col =>
        {
            if (string.IsNullOrEmpty(col)) return;
            if (_sortColumn == col)
                _sortDescending = !_sortDescending;
            else
            {
                _sortColumn = col;
                _sortDescending = false;
            }
            NotifySortIndicators();
            ApplyFilterAndSort();
        });
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
            .ToList();

        var profileLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        var aggregated = new Dictionary<string, (string Name, string ShortName, string IconUrl, int Total, bool FiR)>();

        foreach (var station in stations)
        {
            var currentLevel = profileLevels.TryGetValue(station.Id, out var lvl) ? lvl : 0;

            foreach (var levelRecord in station.Levels.Where(l => l.Level > currentLevel))
            {
                foreach (var req in levelRecord.ItemRequirements)
                {
                    if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                    if (aggregated.TryGetValue(req.TarkovItemId, out var existing))
                        aggregated[req.TarkovItemId] = (existing.Name, existing.ShortName, existing.IconUrl, existing.Total + req.Quantity, existing.FiR || req.FoundInRaid);
                    else
                        aggregated[req.TarkovItemId] = (req.ItemName, req.ShortName, req.IconUrl, req.Quantity, req.FoundInRaid);
                }
            }
        }

        if (aggregated.Count == 0)
        {
            Summary = stations.Count == 0
                ? "No game data — pull hideout data in Settings first."
                : "All stations are fully upgraded!";
            IsEmpty = true;
            return;
        }

        foreach (var kvp in aggregated)
        {
            var iconPath = AppMode.ResolveIconPath(kvp.Value.IconUrl);

            var row = new ItemPoolRow
            {
                TarkovItemId = kvp.Key,
                ItemName = kvp.Value.Name,
                ShortName = kvp.Value.ShortName,
                TotalNeeded = kvp.Value.Total,
                IconPath = iconPath,
                FoundInRaid = kvp.Value.FiR,
                QuantityOwned = itemCounts.TryGetValue(kvp.Key, out var owned) ? owned : 0,
            };
            row.SetOnChanged(SaveItemCount);
            _allRows.Add(row);
        }

        IsEmpty = false;
        NotifySortIndicators();
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var q = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var filter = _searchText.Trim().ToLower();
            q = q.Where(r =>
                r.ItemName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.ShortName.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        q = (_sortColumn, _sortDescending) switch
        {
            ("Needed",   false) => q.OrderBy(r => r.TotalNeeded).ThenBy(r => r.ItemName),
            ("Needed",   true)  => q.OrderByDescending(r => r.TotalNeeded).ThenBy(r => r.ItemName),
            ("Owned",    false) => q.OrderBy(r => r.QuantityOwned).ThenBy(r => r.ItemName),
            ("Owned",    true)  => q.OrderByDescending(r => r.QuantityOwned).ThenBy(r => r.ItemName),
            ("Progress", false) => q.OrderBy(r => r.GatheringPercent).ThenBy(r => r.ItemName),
            ("Progress", true)  => q.OrderByDescending(r => r.GatheringPercent).ThenBy(r => r.ItemName),
            // "Name" default: incomplete first, then alphabetical
            (_,          false) => q.OrderBy(r => r.IsFullyStocked).ThenBy(r => r.ItemName),
            (_,          true)  => q.OrderByDescending(r => r.IsFullyStocked).ThenByDescending(r => r.ItemName),
        };

        Items.Clear();
        foreach (var row in q)
            Items.Add(row);
    }

    private void SaveItemCount(ItemPoolRow row)
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
        Summary = $"{stocked} / {total} items fully stocked";
        GatheringPercent = total == 0 ? 0.0 : (double)stocked / total;
        GatheringLabel = total == 0 ? "" : $"{GatheringPercent:0%}";
    }

    private string SortIndicator(string col) =>
        _sortColumn != col ? "" : (_sortDescending ? " ▼" : " ▲");

    private void NotifySortIndicators()
    {
        OnPropertyChanged(nameof(SortIndicatorName));
        OnPropertyChanged(nameof(SortIndicatorNeeded));
        OnPropertyChanged(nameof(SortIndicatorOwned));
        OnPropertyChanged(nameof(SortIndicatorProgress));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
