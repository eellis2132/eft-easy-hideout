namespace EasyHideout.Models;

public class AppSettings
{
    public int Id { get; set; } = 1;
    public int? ActiveProfileId { get; set; }
    public string DetailPanelPosition { get; set; } = "right";
    public DateTime? LastApiRefresh { get; set; }
    public string Theme { get; set; } = "dark";
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public int? PriorityL1Show { get; set; }
    public int? PriorityL2Show { get; set; }
    public string ItemNameDisplay { get; set; } = "Both";
    public string ApiRefreshMode { get; set; } = "Manual";
    public int ApiRefreshIntervalMinutes { get; set; } = 60;

    public Profile? ActiveProfile { get; set; }
}
