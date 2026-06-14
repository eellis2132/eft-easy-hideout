using EasyHideout.Data;
using EasyHideout.Models;

namespace EasyHideout.Helpers;

public static class EditionBenefits
{
    public static readonly string[] AllEditions =
    [
        "Standard",
        "Left Behind",
        "Prepare for Escape",
        "Edge of Darkness",
        "Unheard",
    ];

    // Maps edition name → the stash level the player starts with
    private static readonly Dictionary<string, int> StashLevel = new()
    {
        ["Standard"]            = 1,
        ["Left Behind"]         = 2,
        ["Prepare for Escape"]  = 3,
        ["Edge of Darkness"]    = 4,
        ["Unheard"]             = 4,
    };

    public static int GetStashLevel(string edition) =>
        StashLevel.TryGetValue(edition, out var lvl) ? lvl : 1;

    /// <summary>
    /// Seeds (or upgrades) the stash station level for a profile based on edition.
    /// Always takes the max of current level and edition bonus — never downgrades.
    /// No-op for Standard (level 1 is the DB default starting state).
    /// </summary>
    public static void Apply(AppDbContext db, int profileId, string edition)
    {
        var stashLevel = GetStashLevel(edition);
        if (stashLevel <= 1) return; // Standard has no bonus to apply

        var stash = db.HideoutStations
            .FirstOrDefault(s => s.NormalizedName == "stash" || s.Name.ToLower() == "stash");
        if (stash == null) return;

        var existing = db.ProfileStationLevels
            .FirstOrDefault(x => x.ProfileId == profileId && x.StationId == stash.Id);

        if (existing != null)
            existing.CurrentLevel = Math.Max(existing.CurrentLevel, stashLevel);
        else
            db.ProfileStationLevels.Add(new ProfileStationLevel
            {
                ProfileId = profileId,
                StationId = stash.Id,
                CurrentLevel = stashLevel
            });
    }
}
