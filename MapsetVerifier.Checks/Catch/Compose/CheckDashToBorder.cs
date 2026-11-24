using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.RankingCriteria;
using static MapsetVerifier.Parser.Objects.Beatmap;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckDashToBorder : BeatmapCheck
{
    /// <summary> Landing this close to the border the catcher moves towards counts as at the border. </summary>
    private const float BorderDistance = 8;

    private const float PlayfieldWidth = 512;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Dashes or hyperdashes to the edge of the playfield.",
            Modes = [Mode.Catch],
            Author = "Greaper",
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out dashes and hyperdashes that land within 8 pixels of the left or right border they move towards."
                },
                {
                    "Reasoning",
                    @"
                    The catcher is stopped abruptly when it reaches the border, which feels uncomfortable at dash speed. Ranked
                    maps rarely land this close to the edge; from 16 pixels away it is common and fine.

                    Overdoses use it more often on purpose, so there it is only a minor issue."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Border",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} lands {2} px from the border.",
                    "timestamp -",
                    "movement",
                    "px"
                )
                    .WithCause(
                        "A dash or hyperdash lands right at the left or right border of the playfield."
                    )
                    .WithRule(RC.Catch.DashesHyperdashesNotUsedDestination)
            },
            {
                "BorderMinor",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} {1} lands {2} px from the border.",
                    "timestamp -",
                    "movement",
                    "px"
                )
                    .WithCause(
                        "A dash or hyperdash in an Overdose lands right at the left or right border."
                    )
                    .WithRule(RC.Catch.DashesHyperdashesNotUsedDestination)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var objects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (var i = 0; i < objects.Count - 1; i++)
        {
            var (current, next) = (objects[i], objects[i + 1]);

            if (
                current.MovementType == CatchMovementType.Walk
                || !float.IsFinite(current.Position.X)
                || !float.IsFinite(next.Position.X)
            )
                continue;

            var direction = Math.Sign(next.Position.X - current.Position.X);

            if (direction == 0)
                continue;

            var toBorder = direction > 0 ? PlayfieldWidth - next.Position.X : next.Position.X;

            if (toBorder > BorderDistance)
                continue;

            var movement =
                current.MovementType == CatchMovementType.Hyperdash ? "Hyperdash" : "Dash";
            var timestamp = CatchExtensions.GetTimestamps(current, next);
            var px = Math.Max(0, Math.Round(toBorder));

            yield return new Issue(
                GetTemplate("Border"),
                beatmap,
                timestamp,
                movement,
                px
            ).ForDifficulties(
                Difficulty.Easy,
                Difficulty.Normal,
                Difficulty.Hard,
                Difficulty.Insane
            );

            yield return new Issue(
                GetTemplate("BorderMinor"),
                beatmap,
                timestamp,
                movement,
                px
            ).ForDifficulties(Difficulty.Expert, Difficulty.Ultra);
        }
    }
}
