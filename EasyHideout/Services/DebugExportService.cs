using System.IO;
using System.Text;
using System.Text.Json;
using EasyHideout.Data;
using EasyHideout.Helpers;
using EasyHideout.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyHideout.Services;

public static class DebugExportService
{
    public static string Export(int profileId)
    {
        using var db = ServiceLocator.Get<AppDbContext>();

        var profile = db.Profiles.Find(profileId);
        var stationLevels = db.ProfileStationLevels
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.StationId, x => x.CurrentLevel);
        var itemCounts = db.ItemCounts
            .Where(x => x.ProfileId == profileId)
            .ToDictionary(x => x.TarkovItemId, x => x.QuantityOwned);

        var stations = db.HideoutStations
            .Include(s => s.Levels)
                .ThenInclude(l => l.ItemRequirements)
            .Include(s => s.Levels)
                .ThenInclude(l => l.StationDependencies)
            .OrderBy(s => s.Name)
            .ToList();

        var distService = ServiceLocator.Get<DistributionService>();
        var completions = distService.ComputeBothTiers(profileId);

        var sb = new StringBuilder();
        sb.AppendLine("=== EASY HIDEOUT DEBUG EXPORT ===");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Profile summary
        sb.AppendLine("--- PROFILE ---");
        sb.AppendLine($"Name:    {profile?.Name ?? "unknown"}");
        sb.AppendLine($"Edition: {profile?.Edition ?? "unknown"}");
        sb.AppendLine();

        // Station levels + next-level item status
        sb.AppendLine("--- STATION STATUS ---");
        sb.AppendLine($"{"Station",-30} {"Lvl",4}  {"MaxLvl",6}  {"L1%",6}  {"L2%",6}  {"Blocked?",8}");
        sb.AppendLine(new string('-', 80));

        foreach (var station in stations)
        {
            var current = stationLevels.TryGetValue(station.Id, out var lvl) ? lvl : 0;
            var l1Pct = completions.L1.TryGetValue(station.Id, out var p1) ? p1 : -1;
            var l2Pct = completions.L2.TryGetValue(station.Id, out var p2) ? p2 : -1;

            var nextLevel = station.Levels.FirstOrDefault(l => l.Level == current + 1);
            var blocked = nextLevel != null && IsBlocked(nextLevel, stationLevels);

            var l1Str = l1Pct < 0 ? "  n/a" : $"{l1Pct,5:0%}";
            var l2Str = l2Pct < 0 ? "  n/a" : $"{l2Pct,5:0%}";
            sb.AppendLine($"{station.Name,-30} {current,4}  {station.MaxLevel,6}  {l1Str}  {l2Str}  {(blocked ? "BLOCKED" : ""),8}");

            // Detail rows: next level requirements
            if (nextLevel != null)
            {
                foreach (var req in nextLevel.ItemRequirements.OrderBy(r => r.ItemName))
                {
                    if (CurrencyFilter.IsCurrency(req.TarkovItemId)) continue;
                    var owned = itemCounts.TryGetValue(req.TarkovItemId, out var qty) ? qty : 0;
                    var status = owned >= req.Quantity ? "OK" : $"{owned}/{req.Quantity}";
                    sb.AppendLine($"  {"",30} [{req.ShortName}] {req.ItemName}: {status}");
                }
            }
        }
        sb.AppendLine();

        // Item counts (non-zero only)
        sb.AppendLine("--- ITEM POOL (non-zero owned) ---");
        var nonZero = itemCounts.Where(x => x.Value > 0).OrderBy(x => x.Key).ToList();
        if (nonZero.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var (itemId, qty) in nonZero)
            {
                var name = db.ItemCounts
                    .Where(x => x.ProfileId == profileId && x.TarkovItemId == itemId)
                    .Select(x => x.ItemName)
                    .FirstOrDefault() ?? itemId;
                sb.AppendLine($"  {qty,5}x  {name} [{itemId}]");
            }
        }
        sb.AppendLine();

        // Distribution completions
        sb.AppendLine("--- DISTRIBUTION COMPLETIONS ---");
        sb.AppendLine($"{"Station",-30}  {"L+1",6}  {"L+2",6}");
        sb.AppendLine(new string('-', 50));
        foreach (var station in stations)
        {
            var hasL1 = completions.L1.TryGetValue(station.Id, out var d1);
            var hasL2 = completions.L2.TryGetValue(station.Id, out var d2);
            if (!hasL1 && !hasL2) continue;
            var l1s = hasL1 ? $"{d1,5:0%}" : "  n/a";
            var l2s = hasL2 ? $"{d2,5:0%}" : "  n/a";
            sb.AppendLine($"{station.Name,-30}  {l1s}  {l2s}");
        }

        return sb.ToString();
    }

    private static bool IsBlocked(HideoutLevel level, Dictionary<int, int> profileLevels)
    {
        foreach (var dep in level.StationDependencies)
        {
            var current = profileLevels.TryGetValue(dep.RequiredStationId, out var v) ? v : 0;
            if (current < dep.RequiredLevel) return true;
        }
        return false;
    }

    public static string SaveToFile(int profileId)
    {
        var content = Export(profileId);
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppMode.AppDataFolder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"debug_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(path, content);
        return path;
    }
}
