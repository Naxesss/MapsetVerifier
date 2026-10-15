using osu.Game.Rulesets.Difficulty.Preprocessing;
using Beatmap = MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     Shared by the <c>Extended*DifficultyCalculator</c>s. Rulesets skip, merge and reorder objects
///     before scoring them, so the only way to know which map times a skill's per-object difficulties
///     belong to is to record the objects at the point the calculator is about to process them.
/// </summary>
internal static class DifficultyObjectTimingCapture
{
    public static List<DifficultyHitObject> Capture(
        IEnumerable<DifficultyHitObject> sortedObjects,
        Beatmap mvBeatmap
    )
    {
        var objects = sortedObjects.ToList();

        mvBeatmap.DifficultyObjectTimings = objects
            .Select(hitObject => new DifficultyObjectTiming(hitObject.StartTime, hitObject.EndTime))
            .ToList();

        return objects;
    }
}
