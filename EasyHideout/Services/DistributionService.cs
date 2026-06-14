using EasyHideout.Data;
using EasyHideout.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.Services;

public record TieredCompletions(Dictionary<int, double> L1, Dictionary<int, double> L2);

public class DistributionService
{
    private record StationNeed(int StationId, List<(string ItemId, int Needed)> Items);

    /// <summary>
    /// Computes per-station completion percentages for L+1 and L+2 tiers separately,
    /// allocating owned items L+1-first, highest naive completion first within each tier.
    /// </summary>
    public TieredCompletions ComputeBothTiers(int profileId)
    {
        using var db = ServiceLocator.Get<AppDbContext>();

        var stations = db.HideoutStations
            .Include(s => s.Levels)
                .ThenInclude(l => l.ItemRequirements)
            .ToList();

        var profileLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);

        var available = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        var l1 = new Dictionary<int, double>();
        var l2 = new Dictionary<int, double>();

        foreach (var (tier, tierResult) in new[] { (1, l1), (2, l2) })
        {
            var needs = new List<StationNeed>();
            foreach (var station in stations)
            {
                var currentLevel = profileLevels.TryGetValue(station.Id, out var lvl) ? lvl : 0;
                var level = station.Levels.FirstOrDefault(l => l.Level == currentLevel + tier);
                if (level == null) continue;

                var items = level.ItemRequirements
                    .Where(r => !CurrencyFilter.IsCurrency(r.TarkovItemId))
                    .Select(r => (ItemId: r.TarkovItemId, Needed: r.Quantity))
                    .ToList();
                if (items.Count == 0) continue;

                needs.Add(new StationNeed(station.Id, items));
            }

            needs = needs
                .OrderByDescending(n => NaiveCompletion(n.Items, available))
                .ToList();

            foreach (var need in needs)
            {
                int totalNeeded = 0, totalAllocated = 0;
                foreach (var (itemId, needed) in need.Items.OrderBy(x => x.Needed))
                {
                    totalNeeded += needed;
                    var avail = available.TryGetValue(itemId, out var v) ? v : 0;
                    var alloc = Math.Min(avail, needed);
                    if (alloc > 0) available[itemId] = avail - alloc;
                    totalAllocated += alloc;
                }
                tierResult[need.StationId] = totalNeeded == 0 ? 1.0 : (double)totalAllocated / totalNeeded;
            }
        }

        return new TieredCompletions(l1, l2);
    }

    // Backward-compatible wrapper used by ActiveNodes
    public Dictionary<int, double> Compute(int profileId) => ComputeBothTiers(profileId).L1;

    private static double NaiveCompletion(List<(string ItemId, int Needed)> items, Dictionary<string, int> available)
    {
        int totalNeeded = 0, totalSatisfied = 0;
        foreach (var (itemId, needed) in items)
        {
            totalNeeded += needed;
            var owned = available.TryGetValue(itemId, out var v) ? v : 0;
            totalSatisfied += Math.Min(owned, needed);
        }
        return totalNeeded == 0 ? 1.0 : (double)totalSatisfied / totalNeeded;
    }
}
