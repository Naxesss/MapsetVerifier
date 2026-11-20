using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Standard.Spread;

[Check]
public class CheckSliderVelocityChanges : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Difficulties = [Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal],
            Category = "Spread",
            Message = "Frequent slider velocity changes.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out Easies and Normals whose slider velocity changes often, more than 8 times a minute in an Easy
                    or 12 times a minute in a Normal."
                },
                {
                    "Reasoning",
                    @"
                    Newer players judge how long to hold a slider by how fast it moves. When that speed keeps changing, they
                    can't rely on it. Only changes between one slider and the next count, so green lines that only change
                    hit sound volume don't.

                    Ranked Easies rarely change it more than twice a minute, and ranked Normals more than five times."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Frequent",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "Slider velocity changes {0} times a minute, more than {1}.",
                    "count",
                    "limit"
                )
                    .WithCause(
                        "The slider velocity changes between sliders more often than ranked lower difficulties do."
                    )
                    .WithRule(
                        RC.Osu.Easy_FrequentlyManipulatingSliderVelocityDiscouraged,
                        RC.Osu.Normal_FrequentlyManipulatingSliderVelocityDiscouraged
                    )
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var perMinute = OsuPatterns.SvChangesPerMinute(beatmap);
        var formatted = perMinute.ToString("0.#", CultureInfo.InvariantCulture);

        if (perMinute > 8)
            yield return new Issue(GetTemplate("Frequent"), beatmap, formatted, 8).ForDifficulties(
                Beatmap.Difficulty.Easy
            );

        if (perMinute > 12)
            yield return new Issue(GetTemplate("Frequent"), beatmap, formatted, 12).ForDifficulties(
                Beatmap.Difficulty.Normal
            );
    }
}
