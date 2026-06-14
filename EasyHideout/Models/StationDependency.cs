namespace EasyHideout.Models;

public class StationDependency
{
    public int Id { get; set; }
    public int HideoutLevelId { get; set; }
    public int RequiredStationId { get; set; }
    public int RequiredLevel { get; set; }

    public HideoutLevel HideoutLevel { get; set; } = null!;
    public HideoutStation RequiredStation { get; set; } = null!;
}
