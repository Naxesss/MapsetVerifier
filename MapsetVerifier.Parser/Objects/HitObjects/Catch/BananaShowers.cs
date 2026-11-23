using MapsetVerifier.Parser.Difficulty;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Mods;
using Serilog;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

/// <summary>
///     Where the bananas of a spinner fall. Their positions look random, but the game picks the same ones every time.
/// </summary>
public static class BananaShowers
{
    /// <summary> A banana, which is optional to catch. </summary>
    public readonly record struct FallingBanana(double Time, float X);

    /// <summary>
    ///     The bananas of every spinner in the beatmap, by start time of the spinner. Empty if osu! can't read the beatmap.
    /// </summary>
    public static Dictionary<double, List<FallingBanana>> GetBananas(Beatmap beatmap)
    {
        try
        {
            // osu! gives up after 10 seconds unless it's given a token of our own.
            using var neverCancelled = new CancellationTokenSource();

            return LocalDifficultyCalculator
                .WorkingBeatmapOf(beatmap)
                .GetPlayableBeatmap(
                    new CatchRuleset().RulesetInfo,
                    Array.Empty<Mod>(),
                    neverCancelled.Token
                )
                .HitObjects.OfType<BananaShower>()
                .GroupBy(shower => shower.StartTime)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        group
                            .First()
                            .NestedHitObjects.OfType<Banana>()
                            .Select(banana => new FallingBanana(
                                banana.StartTime,
                                banana.EffectiveX
                            ))
                            .OrderBy(banana => banana.Time)
                            .ToList()
                );
        }
        catch (Exception exception)
        {
            Log.Warning(
                exception,
                "Couldn't get the banana positions of {Beatmap}",
                beatmap.MapPath
            );

            return [];
        }
    }
}
