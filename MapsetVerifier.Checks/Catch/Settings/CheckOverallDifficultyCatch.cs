using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Catch.Settings;

[Check]
public class CheckOverallDifficultyCatch : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Author = "Greaper",
            Category = "Settings",
            Message = "Overall difficulty differs from approach rate.",
            Modes = [Beatmap.Mode.Catch],
            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Keeping the overall difficulty of osu!catch difficulties the same as their approach rate."
                },
                {
                    "Reasoning",
                    @"
                    Overall difficulty does nothing in osu!catch gameplay, but it is still shown on the website and in-game.
                    Matching it to the approach rate keeps the shown settings consistent and avoids confusion."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Mismatch",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "Overall difficulty is {0}, while approach rate is {1}.",
                    "OD",
                    "AR"
                )
                    .WithCause("The overall difficulty is not the same as the approach rate.")
                    .WithRule(RC.Catch.OverallDifficultySameValueApproach)
            },
        };

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var settings = beatmap.DifficultySettings;

        if (Math.Abs(settings.overallDifficulty - settings.approachRate) < 0.001)
            yield break;

        yield return new Issue(
            GetTemplate("Mismatch"),
            beatmap,
            settings.overallDifficulty.ToString("0.#", CultureInfo.InvariantCulture),
            settings.approachRate.ToString("0.#", CultureInfo.InvariantCulture)
        );
    }
}
