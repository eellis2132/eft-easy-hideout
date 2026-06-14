using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.ViewModels;

public enum AppView { Priority, ActiveNodes, TotalItemPool, Wishlist, Shopping, FocusNode, IgnoredItems, Settings }

public class MainViewModel : INotifyPropertyChanged
{
    private AppView _currentView = AppView.Priority;
    private string _activeProfileName = "No Profile";
    private int? _activeProfileId;

    public TooltipService Tooltip { get; } = ServiceLocator.Get<TooltipService>();

    public AppView CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPriorityActive));
            OnPropertyChanged(nameof(IsActiveNodesActive));
            OnPropertyChanged(nameof(IsTotalItemPoolActive));
            OnPropertyChanged(nameof(IsWishlistActive));
            OnPropertyChanged(nameof(IsShoppingActive));
            OnPropertyChanged(nameof(IsFocusNodeActive));
            OnPropertyChanged(nameof(IsIgnoredItemsActive));
            OnPropertyChanged(nameof(IsSettingsActive));
        }
    }

    public string ActiveProfileName
    {
        get => _activeProfileName;
        set { _activeProfileName = value; OnPropertyChanged(); }
    }

    public int? ActiveProfileId
    {
        get => _activeProfileId;
        set { _activeProfileId = value; OnPropertyChanged(); }
    }

    public bool IsPriorityActive => CurrentView == AppView.Priority;
    public bool IsActiveNodesActive => CurrentView == AppView.ActiveNodes;
    public bool IsTotalItemPoolActive => CurrentView == AppView.TotalItemPool;
    public bool IsWishlistActive => CurrentView == AppView.Wishlist;
    public bool IsShoppingActive => CurrentView == AppView.Shopping;
    public bool IsFocusNodeActive => CurrentView == AppView.FocusNode;
    public bool IsIgnoredItemsActive => CurrentView == AppView.IgnoredItems;
    public bool IsSettingsActive => CurrentView == AppView.Settings;

    public RelayCommand NavPriorityCommand { get; }
    public RelayCommand NavActiveNodesCommand { get; }
    public RelayCommand NavTotalItemPoolCommand { get; }
    public RelayCommand NavWishlistCommand { get; }
    public RelayCommand NavShoppingCommand { get; }
    public RelayCommand NavFocusNodeCommand { get; }
    public RelayCommand NavIgnoredItemsCommand { get; }
    public RelayCommand NavSettingsCommand { get; }
    public RelayCommand NavProfileManagementCommand { get; }

    public MainViewModel()
    {
        NavPriorityCommand = new RelayCommand(() => CurrentView = AppView.Priority);
        NavActiveNodesCommand = new RelayCommand(() => CurrentView = AppView.ActiveNodes);
        NavTotalItemPoolCommand = new RelayCommand(() => CurrentView = AppView.TotalItemPool);
        NavWishlistCommand = new RelayCommand(() => CurrentView = AppView.Wishlist);
        NavShoppingCommand = new RelayCommand(() => CurrentView = AppView.Shopping);
        NavFocusNodeCommand = new RelayCommand(() => CurrentView = AppView.FocusNode);
        NavIgnoredItemsCommand = new RelayCommand(() => CurrentView = AppView.IgnoredItems);
        NavSettingsCommand = new RelayCommand(() => CurrentView = AppView.Settings);
        NavProfileManagementCommand = new RelayCommand(() => CurrentView = AppView.Settings);

        LoadActiveProfile();
        InitAutoRefreshTimer();
    }

    private DispatcherTimer? _autoRefreshTimer;

    private void InitAutoRefreshTimer()
    {
        _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _autoRefreshTimer.Tick += async (_, _) =>
        {
            try { await TryAutoRefreshAsync(); }
            catch { /* swallow — auto-refresh failures are non-fatal */ }
        };
        _autoRefreshTimer.Start();
    }

    public void StopAutoRefreshTimer() => _autoRefreshTimer?.Stop();

    private async Task TryAutoRefreshAsync()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ApiRefreshMode != "Auto") return;

        var intervalMinutes = Math.Max(30, settings.ApiRefreshIntervalMinutes);
        var lastRefresh = settings.LastApiRefresh ?? DateTime.MinValue;
        if (DateTime.UtcNow < lastRefresh.AddMinutes(intervalMinutes)) return;

        var api = ServiceLocator.Get<TarkovApiService>();
        var result = await api.PullHideoutDataAsync();
        if (result.Success)
            AppEvents.RaiseDataRefreshed();
    }

    public void LoadActiveProfile()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings
            .Include(s => s.ActiveProfile)
            .FirstOrDefault(s => s.Id == 1);

        if (settings?.ActiveProfile != null)
        {
            ActiveProfileId = settings.ActiveProfile.Id;
            ActiveProfileName = settings.ActiveProfile.Name;
        }
        else
        {
            ActiveProfileId = null;
            ActiveProfileName = "No Profile";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
