namespace EasyHideout.Helpers;

public static class NameFormatHelper
{
    public static string Current { get; set; } = "Both";

    public static string Apply(string name, string shortName) => Current switch
    {
        "Short" => string.IsNullOrEmpty(shortName) ? name : shortName,
        "Long"  => name,
        _       => string.IsNullOrEmpty(shortName) ? name : $"[{shortName}] {name}",
    };
}
