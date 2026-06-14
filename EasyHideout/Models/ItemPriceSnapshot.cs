namespace EasyHideout.Models;

public class ItemPriceSnapshot
{
    public string TarkovItemId { get; set; } = "";
    public int PreviousAvgPrice { get; set; }
    public DateTime SnapshotAt { get; set; }
}
