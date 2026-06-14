using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using EasyHideout.Services;
using EasyHideout.Views;
using Microsoft.EntityFrameworkCore;
using static EasyHideout.Helpers.EditionBenefits;

namespace EasyHideout.ViewModels;

public class ProfileItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _edition = "Standard";
    private int _characterLevel = 1;
    private bool _isActive;
    private bool _isEditing;
    private string _editName = "";
    private RelayCommand? _incrementCharacterLevelCommand;
    private RelayCommand? _decrementCharacterLevelCommand;

    public int Id { get; set; }
    public Action<ProfileItem>? EditionChanged;
    public Action<ProfileItem>? CharacterLevelChanged;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Edition
    {
        get => _edition;
        set { _edition = value; OnPropertyChanged(); EditionChanged?.Invoke(this); }
    }

    public int CharacterLevel
    {
        get => _characterLevel;
        set
        {
            var clamped = Math.Max(1, Math.Min(79, value));
            if (_characterLevel == clamped) return;
            _characterLevel = clamped;
            OnPropertyChanged();
            CharacterLevelChanged?.Invoke(this);
        }
    }

    // Sets edition without firing the EditionChanged callback (used during load)
    public void InitEdition(string edition) { _edition = edition; OnPropertyChanged(nameof(Edition)); }

    // Sets character level without firing the CharacterLevelChanged callback (used during load)
    public void InitCharacterLevel(int level) { _characterLevel = Math.Max(1, Math.Min(79, level)); OnPropertyChanged(nameof(CharacterLevel)); }

    public RelayCommand IncrementCharacterLevelCommand => _incrementCharacterLevelCommand ??= new RelayCommand(() => CharacterLevel++);
    public RelayCommand DecrementCharacterLevelCommand => _decrementCharacterLevelCommand ??= new RelayCommand(() => CharacterLevel--);

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set { _isEditing = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotEditing)); }
    }

    public bool IsNotEditing => !_isEditing;

    public string EditName
    {
        get => _editName;
        set { _editName = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;
    private string _newProfileName = "";
    private string _newProfileEdition = "Standard";
    private string _apiStatus = "";
    private string _lastSynced = "Never";
    private string _selectedDetailPanelPosition = "right";
    private string _itemNameDisplay = "Both";
    private string _apiRefreshMode = "Manual";
    private int _apiRefreshIntervalMinutes = 60;
    private bool _isApiRunning;

    public string[] AllEditions => EditionBenefits.AllEditions;

    public TooltipService Tooltip => ServiceLocator.Get<TooltipService>();
    public ObservableCollection<ProfileItem> Profiles { get; } = new();

    public string NewProfileName
    {
        get => _newProfileName;
        set { _newProfileName = value; OnPropertyChanged(); CreateProfileCommand.RaiseCanExecuteChanged(); }
    }

    public string NewProfileEdition
    {
        get => _newProfileEdition;
        set { _newProfileEdition = value; OnPropertyChanged(); }
    }

    public string ApiStatus
    {
        get => _apiStatus;
        set { _apiStatus = value; OnPropertyChanged(); }
    }

    public string LastSynced
    {
        get => _lastSynced;
        set { _lastSynced = value; OnPropertyChanged(); }
    }

    public string SelectedDetailPanelPosition
    {
        get => _selectedDetailPanelPosition;
        set { _selectedDetailPanelPosition = value; OnPropertyChanged(); SaveDetailPanelPosition(); }
    }

    public string ItemNameDisplay
    {
        get => _itemNameDisplay;
        set
        {
            if (_itemNameDisplay == value) return;
            _itemNameDisplay = value;
            OnPropertyChanged();
            NameFormatHelper.Current = value;
            SaveItemNameDisplay(value);
            AppEvents.RaiseNameFormatChanged();
        }
    }

    public string ApiRefreshMode
    {
        get => _apiRefreshMode;
        set { if (_apiRefreshMode == value) return; _apiRefreshMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAutoRefresh)); OnPropertyChanged(nameof(IsManualRefresh)); SaveRefreshSettings(); }
    }

    public int ApiRefreshIntervalMinutes
    {
        get => _apiRefreshIntervalMinutes;
        set { if (_apiRefreshIntervalMinutes == value) return; _apiRefreshIntervalMinutes = value; OnPropertyChanged(); SaveRefreshSettings(); }
    }

    public bool IsAutoRefresh => _apiRefreshMode == "Auto";
    public bool IsManualRefresh => _apiRefreshMode != "Auto";
    public int[] RefreshIntervalOptions { get; } = { 30, 60, 120 };

    public bool IsApiRunning
    {
        get => _isApiRunning;
        set { _isApiRunning = value; OnPropertyChanged(); }
    }

    public RelayCommand SetDarkThemeCommand { get; }
    public RelayCommand SetLightThemeCommand { get; }
    public bool IsDarkTheme => ServiceLocator.Get<ThemeService>().CurrentTheme == "dark";
    public bool IsLightTheme => ServiceLocator.Get<ThemeService>().CurrentTheme == "light";
    public RelayCommand PullDataCommand { get; }
    public RelayCommand PullPricesCommand { get; }
    public RelayCommand CreateProfileCommand { get; }
    public RelayCommand<ProfileItem> SwitchProfileCommand { get; }
    public RelayCommand<ProfileItem> StartRenameCommand { get; }
    public RelayCommand<ProfileItem> ConfirmRenameCommand { get; }
    public RelayCommand<ProfileItem> CancelRenameCommand { get; }
    public RelayCommand<ProfileItem> DeleteProfileCommand { get; }
    public RelayCommand ResetProgressCommand { get; }
    public RelayCommand ViewErrorLogCommand { get; }
    public RelayCommand ExportDebugCommand { get; }
    public RelayCommand SetRefreshModeManualCommand { get; }
    public RelayCommand SetRefreshModeAutoCommand { get; }

    public SettingsViewModel(MainViewModel main)
    {
        _main = main;

        SetDarkThemeCommand = new RelayCommand(() => ApplyTheme("dark"));
        SetLightThemeCommand = new RelayCommand(() => ApplyTheme("light"));
        SetRefreshModeManualCommand = new RelayCommand(() => ApiRefreshMode = "Manual");
        SetRefreshModeAutoCommand = new RelayCommand(() => ApiRefreshMode = "Auto");

        PullDataCommand = new RelayCommand(
            async () => await PullDataAsync(),
            () => !IsApiRunning
        );

        PullPricesCommand = new RelayCommand(
            async () => await PullPricesAsync(),
            () => !IsApiRunning
        );

        CreateProfileCommand = new RelayCommand(
            () => CreateProfile(),
            () => !string.IsNullOrWhiteSpace(NewProfileName)
        );

        SwitchProfileCommand = new RelayCommand<ProfileItem>(p =>
        {
            if (p != null) SwitchProfile(p);
        });

        StartRenameCommand = new RelayCommand<ProfileItem>(p =>
        {
            if (p == null) return;
            foreach (var item in Profiles) item.IsEditing = false;
            p.EditName = p.Name;
            p.IsEditing = true;
        });

        ConfirmRenameCommand = new RelayCommand<ProfileItem>(p =>
        {
            if (p == null) return;
            RenameProfile(p);
        });

        CancelRenameCommand = new RelayCommand<ProfileItem>(p =>
        {
            if (p != null) p.IsEditing = false;
        });

        DeleteProfileCommand = new RelayCommand<ProfileItem>(p =>
        {
            if (p != null) DeleteProfile(p);
        });

        ResetProgressCommand = new RelayCommand(ResetProgress);
        ViewErrorLogCommand = new RelayCommand(ViewErrorLog);
        ExportDebugCommand = new RelayCommand(ExportDebug);

        LoadProfiles();
        LoadSettings();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        var profiles = db.Profiles.OrderBy(p => p.CreatedAt).ToList();
        foreach (var p in profiles)
        {
            var item = new ProfileItem
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.Id == settings?.ActiveProfileId,
                EditionChanged = ApplyEditionChange,
                CharacterLevelChanged = SaveCharacterLevel,
            };
            item.InitEdition(p.Edition);
            item.InitCharacterLevel(p.CharacterLevel);
            Profiles.Add(item);
        }
    }

    private void LoadSettings()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings == null) return;

        SelectedDetailPanelPosition = settings.DetailPanelPosition ?? "right";
        LastSynced = settings.LastApiRefresh.HasValue
            ? settings.LastApiRefresh.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "Never";

        _itemNameDisplay = settings.ItemNameDisplay ?? "Both";
        OnPropertyChanged(nameof(ItemNameDisplay));
        NameFormatHelper.Current = _itemNameDisplay;

        _apiRefreshMode = settings.ApiRefreshMode ?? "Manual";
        OnPropertyChanged(nameof(ApiRefreshMode));
        OnPropertyChanged(nameof(IsAutoRefresh));
        _apiRefreshIntervalMinutes = settings.ApiRefreshIntervalMinutes > 0 ? settings.ApiRefreshIntervalMinutes : 60;
        OnPropertyChanged(nameof(ApiRefreshIntervalMinutes));
    }

    private void SaveItemNameDisplay(string value)
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null) return;
            settings.ItemNameDisplay = value;
            db.SaveChanges();
        }
        catch { }
    }

    private void ApplyTheme(string theme)
    {
        ServiceLocator.Get<ThemeService>().Apply(theme);
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));

        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings != null) { settings.Theme = theme; db.SaveChanges(); }
    }

    private async Task PullDataAsync()
    {
        IsApiRunning = true;
        ApiStatus = "Pulling data...";

        var progress = new Progress<string>(msg => ApiStatus = msg);
        var api = ServiceLocator.Get<TarkovApiService>();
        var result = await api.PullHideoutDataAsync(progress);

        IsApiRunning = false;
        ApiStatus = result.Message;

        if (result.Success)
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            LastSynced = settings?.LastApiRefresh.HasValue == true
                ? settings.LastApiRefresh.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "Never";
            AppEvents.RaiseDataRefreshed();
        }
    }

    private async Task PullPricesAsync()
    {
        IsApiRunning = true;
        ApiStatus = "Refreshing prices...";

        var progress = new Progress<string>(msg => ApiStatus = msg);
        var api = ServiceLocator.Get<TarkovApiService>();
        var result = await api.PullPricesOnlyAsync(progress);

        IsApiRunning = false;
        ApiStatus = result.Message;

        if (result.Success)
            AppEvents.RaiseDataRefreshed();
    }

    private void CreateProfile()
    {
        var name = NewProfileName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        using var db = ServiceLocator.Get<AppDbContext>();
        if (db.Profiles.Any(p => p.Name == name))
        {
            ApiStatus = $"A profile named \"{name}\" already exists.";
            return;
        }

        var profile = new Profile { Name = name, Edition = NewProfileEdition };
        db.Profiles.Add(profile);
        db.SaveChanges();

        EditionBenefits.Apply(db, profile.Id, profile.Edition);
        db.SaveChanges();

        NewProfileName = "";
        LoadProfiles();
    }

    private void ApplyEditionChange(ProfileItem item)
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var profile = db.Profiles.Find(item.Id);
        if (profile == null) return;

        profile.Edition = item.Edition;
        EditionBenefits.Apply(db, item.Id, item.Edition);
        db.SaveChanges();
    }

    private void SaveCharacterLevel(ProfileItem item)
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var profile = db.Profiles.Find(item.Id);
            if (profile == null) return;
            profile.CharacterLevel = item.CharacterLevel;
            db.SaveChanges();
        }
        catch { }
    }

    private void SwitchProfile(ProfileItem item)
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings == null) return;

        settings.ActiveProfileId = item.Id;
        db.SaveChanges();

        foreach (var p in Profiles) p.IsActive = p.Id == item.Id;
        _main.ActiveProfileId = item.Id;
        _main.ActiveProfileName = item.Name;
    }

    private void RenameProfile(ProfileItem item)
    {
        var name = item.EditName.Trim();
        if (string.IsNullOrEmpty(name)) { item.IsEditing = false; return; }

        using var db = ServiceLocator.Get<AppDbContext>();
        var profile = db.Profiles.Find(item.Id);
        if (profile == null) { item.IsEditing = false; return; }

        profile.Name = name;
        db.SaveChanges();

        item.Name = name;
        item.IsEditing = false;

        if (item.IsActive)
            _main.ActiveProfileName = name;
    }

    private void DeleteProfile(ProfileItem item)
    {
        var owner = Application.Current.MainWindow;
        bool confirmed = ConfirmationDialog.Show(
            $"This will permanently delete the profile \"{item.Name}\" and all associated progress. This cannot be undone. Continue?",
            owner!
        );
        if (!confirmed) return;

        using var db = ServiceLocator.Get<AppDbContext>();

        db.ProfileStationLevels.Where(x => x.ProfileId == item.Id).ExecuteDelete();
        db.ItemCounts.Where(x => x.ProfileId == item.Id).ExecuteDelete();
        db.ImportantItems.Where(x => x.ProfileId == item.Id).ExecuteDelete();
        db.IgnoredItems.Where(x => x.ProfileId == item.Id).ExecuteDelete();
        db.FocusNodes.Where(x => x.ProfileId == item.Id).ExecuteDelete();

        var profile = db.Profiles.Find(item.Id);
        if (profile != null) db.Profiles.Remove(profile);

        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == item.Id)
        {
            var next = db.Profiles.Where(p => p.Id != item.Id).FirstOrDefault();
            settings.ActiveProfileId = next?.Id;
        }
        db.SaveChanges();

        LoadProfiles();
        _main.LoadActiveProfile();
    }

    private void ResetProgress()
    {
        if (_main.ActiveProfileId == null) return;
        var profileName = _main.ActiveProfileName;
        var owner = Application.Current.MainWindow;

        bool confirmed = ConfirmationDialog.Show(
            $"This will reset all node levels and item counts for \"{profileName}\". This cannot be undone. Continue?",
            owner!
        );
        if (!confirmed) return;

        using var db = ServiceLocator.Get<AppDbContext>();
        var profileId = _main.ActiveProfileId.Value;

        db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ExecuteUpdate(s => s.SetProperty(x => x.CurrentLevel, 0));

        db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ExecuteUpdate(s => s.SetProperty(x => x.QuantityOwned, 0));

        db.SaveChanges();

        // Re-apply edition bonuses so stash starts at the correct level
        var profile = db.Profiles.Find(profileId);
        if (profile != null)
        {
            EditionBenefits.Apply(db, profileId, profile.Edition);
            db.SaveChanges();
        }

        ApiStatus = "Progress reset successfully.";
    }

    private void SaveRefreshSettings()
    {
        try
        {
            using var db = ServiceLocator.Get<AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null) return;
            settings.ApiRefreshMode = _apiRefreshMode;
            settings.ApiRefreshIntervalMinutes = _apiRefreshIntervalMinutes;
            db.SaveChanges();
        }
        catch { }
    }

    private void SaveDetailPanelPosition()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings == null) return;
        settings.DetailPanelPosition = _selectedDetailPanelPosition;
        db.SaveChanges();
    }

    private void ViewErrorLog()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logPath = System.IO.Path.Combine(appData, EasyHideout.Helpers.AppMode.AppDataFolder, "logs", "api_log.txt");
        if (System.IO.File.Exists(logPath))
            System.Diagnostics.Process.Start("notepad.exe", logPath);
        else
            ApiStatus = "No log file found yet.";
    }

    private void ExportDebug()
    {
        using var db = ServiceLocator.Get<AppDbContext>();
        var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
        if (settings?.ActiveProfileId == null)
        {
            ApiStatus = "No active profile — select a profile first.";
            return;
        }
        try
        {
            var path = DebugExportService.SaveToFile(settings.ActiveProfileId.Value);
            System.Diagnostics.Process.Start("notepad.exe", path);
        }
        catch (Exception ex)
        {
            ApiStatus = $"Export failed: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
