using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Tests.Catch;

/// <summary>
/// Catch beatmaps with movement set by hand, since what becomes a dash is up to the parser and has its own tests.
/// </summary>
internal static class CatchObjects
{
    /// <summary> Fruits at the given times, going from one side to the other. </summary>
    public static OsuBuilder Beatmap(params int[] times) =>
        new OsuBuilder()
            .Mode(Parser.Objects.Beatmap.Mode.Catch)
            .WithDefaultTiming()
            .HitObjects(times.Select((time, i) => TestHitObjects.Circle(time, 100 + i % 2 * 100)));

    /// <summary> Sets the movement of the first objects, and all others to walk. </summary>
    public static void Set(CheckTestContext context, params CatchMovementType[] movements)
    {
        var objects = context.GetBeatmap("Test").HitObjects.Cast<ICatchHitObject>().ToList();

        for (var i = 0; i < objects.Count; ++i)
            objects[i].MovementType = i < movements.Length ? movements[i] : CatchMovementType.Walk;
    }
}
