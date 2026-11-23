using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckComboLength : BeatmapCheck
{
    private const int MaxComboLength = 16;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Too long combos.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out combos of more than 16 objects in Easies and Normals."
                },
                {
                    "Reasoning",
                    @"
                    Combo numbers help newer players count the rhythm and see where a pattern starts. Very long combos make the
                    numbers hard to read and lose that help. Ranked Easies and Normals rarely go above 12 objects in a combo.

                    Combos that are too short are left to your own judgement, since short combos are often used on purpose to
                    follow the music."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Long",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} Combo of {1} objects, more than 16.",
                    "timestamp -",
                    "count"
                )
                    .WithCause("A combo in an Easy or Normal has more than 16 objects.")
                    .WithRule(
                        RC.Osu.Easy_EnsureCombosNotUnreasonablyShort,
                        RC.Osu.Normal_EnsureCombosNotUnreasonablyShort
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap) =>
        OsuPatterns
            .Combos(beatmap)
            .Where(run => run.Count > MaxComboLength)
            .Select(run => new Issue(
                GetTemplate("Long"),
                beatmap,
                Timestamp.Get(run.First, run.Last),
                run.Count
            ));
}
