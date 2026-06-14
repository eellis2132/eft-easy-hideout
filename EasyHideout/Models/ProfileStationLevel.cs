namespace EasyHideout.Models;

public class ProfileStationLevel
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int StationId { get; set; }
    public int CurrentLevel { get; set; } = 0;

    public Profile Profile { get; set; } = null!;
    public HideoutStation Station { get; set; } = null!;
}
