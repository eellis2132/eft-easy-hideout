namespace EasyHideout.Models;

public class Profile
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Edition { get; set; } = "Standard";
    public int CharacterLevel { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProfileStationLevel> StationLevels { get; set; } = new List<ProfileStationLevel>();
    public ICollection<ItemCount> ItemCounts { get; set; } = new List<ItemCount>();
    public ICollection<ImportantItem> ImportantItems { get; set; } = new List<ImportantItem>();
    public ICollection<IgnoredItem> IgnoredItems { get; set; } = new List<IgnoredItem>();
    public FocusNode? FocusNode { get; set; }
}
