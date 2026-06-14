namespace EasyHideout.Models;

public class FocusNode
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public int StationId { get; set; }
    public int TargetLevel { get; set; }

    public Profile Profile { get; set; } = null!;
    public HideoutStation Station { get; set; } = null!;
}
