namespace EasyHideout.Models;

public class TraderLoyaltyLevel
{
    public int Id { get; set; }
    public string TraderId { get; set; } = "";
    public string TraderName { get; set; } = "";
    public int LoyaltyLevel { get; set; }
    public int RequiredPlayerLevel { get; set; }
}
