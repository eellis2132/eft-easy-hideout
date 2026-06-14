namespace EasyHideout.Models;

public class HideoutStation
{
    public int Id { get; set; }
    public string TarkovStationId { get; set; } = "";
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public int MaxLevel { get; set; }

    public ICollection<HideoutLevel> Levels { get; set; } = new List<HideoutLevel>();
    public ICollection<ProfileStationLevel> ProfileStationLevels { get; set; } = new List<ProfileStationLevel>();
}
