using EasyHideout.Models;

namespace EasyHideout.Helpers;

public static class BlockingHelper
{
    public static bool IsBlocked(
        HideoutLevel level,
        Dictionary<int, int> profileLevels,
        int characterLevel,
        Dictionary<(string TraderId, int LL), int> traderLookup) =>
        GetBlockReasons(level, profileLevels, characterLevel, traderLookup).Any();

    // Each reason: (StationId for navigation, display text, isNavigable).
    // StationId=0 + isNavigable=false means a trader/non-station block.
    public static IEnumerable<(int StationId, string Text, bool IsNavigable)> GetBlockReasons(
        HideoutLevel level,
        Dictionary<int, int> profileLevels,
        int characterLevel,
        Dictionary<(string TraderId, int LL), int> traderLookup)
    {
        foreach (var dep in level.StationDependencies)
        {
            var dl = profileLevels.TryGetValue(dep.RequiredStationId, out var d) ? d : 0;
            if (dl < dep.RequiredLevel)
                yield return (dep.RequiredStationId, $"{dep.RequiredStation.Name} Level {dep.RequiredLevel}", true);
        }

        foreach (var req in level.TraderRequirements)
        {
            var key = (req.TraderId, req.RequiredLoyaltyLevel);
            var reqPlayerLevel = traderLookup.TryGetValue(key, out var rpl) ? rpl : 0;
            // Only block if we have data (reqPlayerLevel > 0) and character level is insufficient
            if (reqPlayerLevel > 0 && characterLevel < reqPlayerLevel)
                yield return (0, $"{req.TraderName} LL{req.RequiredLoyaltyLevel}  ·  PMC {reqPlayerLevel} required  (+{reqPlayerLevel - characterLevel} levels)", false);
        }
    }
}
