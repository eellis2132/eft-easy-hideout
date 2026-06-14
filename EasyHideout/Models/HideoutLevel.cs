namespace EasyHideout.Models;

public class HideoutLevel
{
    public int Id { get; set; }
    public int StationId { get; set; }
    public int Level { get; set; }

    public HideoutStation Station { get; set; } = null!;
    public ICollection<ItemRequirement> ItemRequirements { get; set; } = new List<ItemRequirement>();
    public ICollection<StationDependency> StationDependencies { get; set; } = new List<StationDependency>();
    public ICollection<TraderRequirement> TraderRequirements { get; set; } = new List<TraderRequirement>();
}
