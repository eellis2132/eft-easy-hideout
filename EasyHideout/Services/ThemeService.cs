using System.Windows;

namespace EasyHideout.Services;

public class ThemeService
{
    private const string DarkSource = "Themes/Dark.xaml";
    private const string LightSource = "Themes/Light.xaml";

    public string CurrentTheme { get; private set; } = "dark";

    public void Apply(string theme)
    {
        CurrentTheme = theme.ToLower();
        var source = CurrentTheme == "light" ? LightSource : DarkSource;
        var uri = new Uri(source, UriKind.Relative);

        var resources = Application.Current.Resources;
        var merged = resources.MergedDictionaries;

        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Dark.xaml") == true ||
            d.Source?.OriginalString.EndsWith("Light.xaml") == true);

        var newDict = new ResourceDictionary { Source = uri };

        if (existing != null)
        {
            var idx = merged.IndexOf(existing);
            merged[idx] = newDict;
        }
        else
        {
            merged.Insert(0, newDict);
        }
    }

    public void Toggle()
    {
        var next = CurrentTheme == "dark" ? "light" : "dark";
        Apply(next);
        Save(next);
    }

    private static void Save(string theme)
    {
        try
        {
            using var db = ServiceLocator.Get<EasyHideout.Data.AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings == null) return;
            settings.Theme = theme;
            db.SaveChanges();
        }
        catch { }
    }

    public void LoadFromDb()
    {
        try
        {
            using var db = ServiceLocator.Get<EasyHideout.Data.AppDbContext>();
            var settings = db.AppSettings.FirstOrDefault(s => s.Id == 1);
            if (settings != null)
                Apply(settings.Theme ?? "dark");
        }
        catch { }
    }
}
