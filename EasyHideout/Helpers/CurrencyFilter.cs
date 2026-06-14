namespace EasyHideout.Helpers;

public static class CurrencyFilter
{
    private static readonly HashSet<string> Ids = new()
    {
        "5449016a4bdc2d6f028b456f", // Roubles
        "5696686a4bdc2da3298b456a", // Dollars
        "569668774bdc2da2298b4568", // Euros
    };

    public static bool IsCurrency(string tarkovItemId) => Ids.Contains(tarkovItemId);
}
