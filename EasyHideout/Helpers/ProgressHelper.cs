using EasyHideout.Models;

namespace EasyHideout.Helpers;

public static class ProgressHelper
{
    /// <summary>
    /// Non-greedy per-station completion: counts each item independently against owned inventory.
    /// Used for priority scoring (does not cross-deduct shared items between stations).
    /// </summary>
    public static double NaiveCompletion(ICollection<ItemRequirement> reqs, Dictionary<string, int> itemCounts)
    {
        int totalNeeded = 0, totalOwned = 0;
        foreach (var req in reqs)
        {
            totalNeeded += req.Quantity;
            var owned = itemCounts.TryGetValue(req.TarkovItemId, out var qty) ? qty : 0;
            totalOwned += Math.Min(owned, req.Quantity);
        }
        return totalNeeded == 0 ? 1.0 : (double)totalOwned / totalNeeded;
    }
}
