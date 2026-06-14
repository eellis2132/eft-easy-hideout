namespace EasyHideout.Models;

public class ItemCount
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string TarkovItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public int QuantityOwned { get; set; } = 0;

    public Profile Profile { get; set; } = null!;
}
