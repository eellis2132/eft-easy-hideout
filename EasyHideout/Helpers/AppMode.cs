using System.IO;

namespace EasyHideout.Helpers;

public static class AppMode
{
#if DEVBUILD
    public const bool IsDev = true;
    public const string AppDataFolder = "EasyHideoutDev";
    public const string WindowTitle = "Easy Hideout [DEV]";
#else
    public const bool IsDev = false;
    public const string AppDataFolder = "EasyHideout";
    public const string WindowTitle = "Easy Hideout";
#endif

    public static string IconDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppDataFolder, "icons");

    public static string ResolveIconPath(string? iconUrl)
    {
        if (string.IsNullOrEmpty(iconUrl)) return "";
        var file = Path.GetFileName(new Uri(iconUrl).LocalPath);
        return string.IsNullOrEmpty(file) ? "" : Path.Combine(IconDir, file);
    }
}
