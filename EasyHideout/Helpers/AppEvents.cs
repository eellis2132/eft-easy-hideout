namespace EasyHideout.Helpers;

public static class AppEvents
{
    public static event Action? NameFormatChanged;
    public static void RaiseNameFormatChanged() => NameFormatChanged?.Invoke();

    public static event Action? DataRefreshed;
    public static void RaiseDataRefreshed() => DataRefreshed?.Invoke();
}
