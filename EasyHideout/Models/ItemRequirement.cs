namespace EasyHideout.Models;

public class ItemRequirement
{
    public int Id { get; set; }
    public int HideoutLevelId { get; set; }
    public string TarkovItemId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string ShortName { get; set; } = "";
    public int Quantity { get; set; }
    public string IconUrl { get; set; } = "";
    public bool FoundInRaid { get; set; } = false;
    public int MinLevelForFlea { get; set; } = 0;
    public int AvgPrice { get; set; } = 0;

    public HideoutLevel HideoutLevel { get; set; } = null!;
}
