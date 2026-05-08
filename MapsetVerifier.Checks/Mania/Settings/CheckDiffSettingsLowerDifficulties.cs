using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Mania.Settings
{
    [Check]
    public class CheckDiffSettingsLowerDifficulties : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Difficulties =
                [
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal,
                    Beatmap.Difficulty.Hard,
                ],
                Category = "Settings",
                Message = "OD/HP Values too high",
                Author = "RandomeLoL",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Prevent easier difficulties to go beyond the OD/HP values present in the ruleset's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    To prevent Easy/Normal/Hard maps from being too difficult, hard limits for both their Overall Difficulty and Health Points were added. These values can be whatever so long they do not go beyond these prestablished limits."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                    "HP Problem",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} should not have a HP value over {1}, currently {2}.",
                        "difficulty",
                        "max hp",
                        "current hp"
                    ).WithCause("One of the difficulties' HP breaches the RC limits.")
                },
                {
                    "OD Problem",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} should not have an OD value over {1}, currently {2}.",
                        "difficulty",
                        "max od",
                        "current od"
                    ).WithCause("One of the difficulties' OD breaches the RC limits.")
                },
                {
                    "Ambiguous",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "Difficulty name {0} is ambiguous. Please ensure that HP {1} and OD {2} make sense to use.",
                        "difficulty",
                        "current hp",
                        "current od"
                    ).WithCause("One of the difficulties uses an ambiguous naming schema.")
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // HP/OD Getters
            var hp = Math.Round(beatmap.DifficultySettings.hpDrain, 2, MidpointRounding.ToEven);
            var od = Math.Round(
                beatmap.DifficultySettings.overallDifficulty,
                2,
                MidpointRounding.ToEven
            );

            // Difficulty name getter
            var diffName = beatmap.MetadataSettings.version;

            // Get current diff for the "switch" to evaluate
            var difficulty = beatmap.GetDifficulty();

            switch (difficulty)
            {
                case Beatmap.Difficulty.Easy:
                {
                    if (hp > 7)
                        yield return new Issue(GetTemplate("HP Problem"), beatmap, diffName, 7, hp);
                    if (od > 7)
                        yield return new Issue(GetTemplate("OD Problem"), beatmap, diffName, 7, od);
                    break;
                }
                case Beatmap.Difficulty.Normal:
                {
                    if (hp > 7.5)
                        yield return new Issue(
                            GetTemplate("HP Problem"),
                            beatmap,
                            diffName,
                            7.5,
                            hp
                        );
                    if (od > 7.5)
                        yield return new Issue(
                            GetTemplate("OD Problem"),
                            beatmap,
                            diffName,
                            7.5,
                            od
                        );
                    break;
                }
                case Beatmap.Difficulty.Hard:
                {
                    if (hp > 8)
                        yield return new Issue(GetTemplate("HP Problem"), beatmap, diffName, 8, hp);
                    if (od > 8)
                        yield return new Issue(GetTemplate("OD Problem"), beatmap, diffName, 8, od);
                    break;
                }
                case Beatmap.Difficulty.Ultra: // Ambiguous difficulty name case.
                {
                    yield return new Issue(GetTemplate("Ambiguous"), beatmap, diffName, hp, od);
                    break;
                }
            }
        }
    }
}
