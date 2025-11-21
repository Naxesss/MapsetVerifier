using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckHyperdashSnap : BeatmapCheck
{
    private const int PlatterMinSnapMs = 125;
    private const int RainMinSnapMs = 62;
    
    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Disallowed hyperdash snap.",
        Modes = [Beatmap.Mode.Catch],
        Difficulties = [Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane],
        Author = "Greaper",

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                Hyperdashes may only be used when the snapping is above or equal to the allowed threshold.

                - For Platters at least *125ms or higher* must be between the ticks of the desired snapping.
                - For Rains at least *62ms or higher* must be between the ticks of the desired snapping."
            },
            {
                "Reason",
                @"
                To ensure an increase of complexity in each level, hyperdashes can be really harsh on lower difficulties."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            { "HyperdashSnap",
                new IssueTemplate(Issue.Level.Problem,
                        "{0} Current snap is not allowed, only ticks with at least {1}ms are allowed, currently {2}ms.",
                        "timestamp - ", "allowed", "current")
                    .WithCause("The used snap is not allowed.")
            }
        };
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        if (catchObjects.Count < 2)
            yield break;

        for (var i = 0; i < catchObjects.Count - 1; i++)
        {
            var current = catchObjects[i];
            if (current.MovementType != CatchMovementType.Hyperdash)
                continue;

            var destination = catchObjects[i + 1];
            var snapMs = destination.Time - current.Time;
            if (snapMs <= 0)
                continue;

            // Round snap to nearest integer to be as close to osu-stable.
            var snapRounded = (int) snapMs;

            if (snapMs < PlatterMinSnapMs)
            {
                yield return new Issue(
                    GetTemplate("HyperdashSnap"),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, destination),
                    PlatterMinSnapMs,
                    snapRounded
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }

            if (snapMs < RainMinSnapMs)
            {
                yield return new Issue(
                    GetTemplate("HyperdashSnap"),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, destination),
                    RainMinSnapMs,
                    snapRounded
                ).ForDifficulties(Beatmap.Difficulty.Insane);
            }
        }
    }
}