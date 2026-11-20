using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Catch.Compose.Rain;

[Check]
public class CheckRainHigherSnappedHyperdash : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Higher-snapped hyperdashes used in hard patterns.",
            Modes = [Mode.Catch],
            Difficulties = [Difficulty.Insane],
            Author = "Greaper",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out higher-snapped hyperdashes in Rains that are used within a slider, together with a
                    higher-snapped dash, or followed by an antiflow dash."
                },
                {
                    "Reasoning",
                    @"
                    Higher-snapped hyperdashes already ask a lot of the player at this level. Inside a slider the path is hard to
                    follow, and dashes right around them, especially ones going back the other way, leave too little time to
                    recover. A higher-snapped dash may come before such a hyperdash, as long as it doesn't go back the other way.

                    Movement along a slider that a hyperdash lands on follows the slider's path, so it isn't counted as a dash
                    after the hyperdash. Ranked Rains often combine higher-snapped hyperdashes with dashes, so those are only
                    minor issues; using one within a slider is a problem."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Slider",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} Higher-snapped hyperdash within a slider.",
                    "timestamp -"
                )
                    .WithCause(
                        "A higher-snapped hyperdash goes between two parts of the same slider."
                    )
                    .WithRule(RC.Catch.Rain_HyperdashesHigherSnappedNotUsed2)
            },
            {
                "WithDash",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Higher-snapped hyperdash {1}.",
                    "timestamp -",
                    "pattern"
                )
                    .WithCause(
                        "A higher-snapped hyperdash is followed by a higher-snapped dash, or comes after one that goes back the other way."
                    )
                    .WithRule(RC.Catch.Rain_HyperdashesHigherSnappedNotUsed3)
            },
            {
                "Antiflow",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Higher-snapped hyperdash followed by an antiflow dash.",
                    "timestamp -"
                )
                    .WithCause("The dash after a higher-snapped hyperdash goes back the other way.")
                    .WithRule(RC.Catch.Rain_HyperdashesHigherSnappedNotFollowed)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var objects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (var i = 0; i < objects.Count - 1; i++)
        {
            var (current, next) = (objects[i], objects[i + 1]);

            if (
                current.MovementType != CatchMovementType.Hyperdash
                || !current.IsHigherSnapped(next, Difficulty.Insane)
            )
                continue;

            if (CheckDashPatterns.InSameSlider(current, next))
                yield return Report("Slider", beatmap, current, next);

            var previous = i > 0 ? objects[i - 1] : null;
            var afterNext = i + 2 < objects.Count ? objects[i + 2] : null;

            // Movement along the slider a hyperdash lands on follows the slider's path, not the mapper's dash choice.
            if (
                afterNext != null
                && next.MovementType != CatchMovementType.Walk
                && !CheckDashPatterns.InSameSlider(next, afterNext)
            )
            {
                if (
                    next.MovementType == CatchMovementType.Dash
                    && next.IsHigherSnapped(afterNext, Difficulty.Insane)
                )
                    yield return Report(
                        "WithDash",
                        beatmap,
                        current,
                        next,
                        afterNext,
                        "followed by a higher-snapped dash"
                    );
                else if (
                    next.MovementType == CatchMovementType.Dash
                    && CheckDashPatterns.IsAntiflow(current, next, afterNext)
                )
                    yield return Report("Antiflow", beatmap, current, next, afterNext);
            }

            if (
                previous is { MovementType: CatchMovementType.Dash }
                && previous.IsHigherSnapped(current, Difficulty.Insane)
                && CheckDashPatterns.IsAntiflow(previous, current, next)
            )
                yield return Report(
                    "WithDash",
                    beatmap,
                    previous,
                    current,
                    next,
                    "after a higher-snapped dash going the other way"
                );
        }
    }

    private Issue Report(string template, Beatmap beatmap, params object[] parts)
    {
        var objects = parts.OfType<ICatchHitObject>().ToArray();
        var extra = parts.Where(part => part is not ICatchHitObject).ToArray();

        return new Issue(
            GetTemplate(template),
            beatmap,
            [CatchExtensions.GetTimestamps(objects), .. extra]
        ).ForDifficulties(Difficulty.Insane);
    }
}
