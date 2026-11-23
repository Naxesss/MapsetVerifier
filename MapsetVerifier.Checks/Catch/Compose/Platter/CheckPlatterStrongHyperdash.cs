using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Catch.Compose.Platter;

[Check]
public class CheckPlatterStrongHyperdash : BeatmapCheck
{
    /// <summary> Times the trigger distance any hyperdash should stay within. </summary>
    private const double Limit = 1.5;

    /// <summary> Times the trigger distance a higher-snapped hyperdash should stay within. </summary>
    private const double HigherSnappedLimit = 1.3;

    /// <summary> Hyperdashes of at least this many milliseconds are basic-snapped. </summary>
    private const int BasicSnappedGap = 250;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Strong hyperdashes.",
            Modes = [Mode.Catch],
            Difficulties = [Difficulty.Hard],
            Author = "Greaper",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out hyperdashes in Platters that are much longer than needed to become a hyperdash.

| Hyperdash | Minor issue | Warning |
|---|---|---|
| Basic-snapped (250 ms or more) | | more than 1.5 times the trigger distance |
| Higher-snapped (less than 250 ms) | more than 1.3 times | more than 1.5 times |

                    Difficulties whose name doesn't say their level are left out unless a level is chosen."
                },
                {
                    "Reasoning",
                    @"
                    The longer a hyperdash, the faster the catcher flies towards the next object. Platters introduce players to
                    hyperdashes, who still have to learn when to let go.

                    The trigger distance is the shortest distance that makes a hyperdash, which depends on the time between the
                    objects and the movement before. The ranking criteria no longer say what makes a hyperdash strong. The
                    limits here are the ones they recommended until October 2025, which ranked Platters still mostly follow. A
                    few use somewhat longer higher-snapped hyperdashes, so those are only a minor issue."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Strong",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Strong hyperdash from {1}, {2} times the trigger distance, more than {3}.",
                    "timestamp -",
                    "object",
                    "times",
                    "limit"
                )
                    .WithCause("A hyperdash is more than 1.5 times the trigger distance.")
                    .WithRule(RC.Catch.Platter_StrongHyperdashesNotUsed)
            },
            {
                "StrongHigher",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Strong higher-snapped hyperdash from {1}, {2} times the trigger distance, more than {3}.",
                    "timestamp -",
                    "object",
                    "times",
                    "limit"
                )
                    .WithCause(
                        "A hyperdash of less than 250 ms is more than 1.3 times the trigger distance."
                    )
                    .WithRule(RC.Catch.Platter_StrongHyperdashesNotUsed)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // The limits are meant for Platters, and a level guessed from star rating isn't reliable enough.
        if (beatmap.IsDifficultyGuessed())
            yield break;

        var objects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (var i = 0; i < objects.Count - 1; i++)
        {
            var (current, next) = (objects[i], objects[i + 1]);
            var triggerDistance = current.GetTriggerDistance(next);

            // Not a hyperdash, or objects so close in time that any distance makes one.
            if (triggerDistance <= 0)
                continue;

            var times = Math.Abs(next.Position.X - current.Position.X) / triggerDistance;
            var isHigherSnapped = next.Time - current.Time < BasicSnappedGap;

            if (times > Limit)
                yield return CreateIssue("Strong", Limit);
            else if (isHigherSnapped && times > HigherSnappedLimit)
                yield return CreateIssue("StrongHigher", HigherSnappedLimit);

            continue;

            Issue CreateIssue(string template, double limit) =>
                new Issue(
                    GetTemplate(template),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, next),
                    current.GetNoteTypeName().ToLower(),
                    // Rounded up to never show the limit itself.
                    (Math.Ceiling(times * 100) / 100).ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    ),
                    limit.ToString(CultureInfo.InvariantCulture)
                ).ForDifficulties(Difficulty.Hard);
        }
    }
}
