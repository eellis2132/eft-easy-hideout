namespace EasyHideout.Models;

public class ImportantItem
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string TarkovItemId { get; set; } = "";
    public string ItemName { get; set; } = "";

    public Profile Profile { get; set; } = null!;
}
