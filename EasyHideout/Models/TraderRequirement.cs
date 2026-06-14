namespace EasyHideout.Models;

public class TraderRequirement
{
    public int Id { get; set; }
    public int HideoutLevelId { get; set; }
    public string TraderId { get; set; } = "";
    public string TraderName { get; set; } = "";
    public int RequiredLoyaltyLevel { get; set; }

    public HideoutLevel HideoutLevel { get; set; } = null!;
}
