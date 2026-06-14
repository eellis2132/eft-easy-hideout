namespace EasyHideout.Models;

public class IgnoredItem
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string TarkovItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string? Note { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    public Profile Profile { get; set; } = null!;
}
