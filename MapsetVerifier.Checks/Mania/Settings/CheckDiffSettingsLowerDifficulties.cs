using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Mania.Settings
{
    [Check]
    public class CheckDiffSettingsLowerDifficulties : RangeDifficultySettingsCheck
    {
        private static readonly Dictionary<Beatmap.Difficulty, SettingRange> HpDrainRanges = new()
        {
            { Beatmap.Difficulty.Easy, new SettingRange(null, 7) },
            { Beatmap.Difficulty.Normal, new SettingRange(null, 7.5) },
            { Beatmap.Difficulty.Hard, new SettingRange(null, 8) },
        };

        private static readonly Dictionary<
            Beatmap.Difficulty,
            SettingRange
        > OverallDifficultyRanges = new()
        {
            { Beatmap.Difficulty.Easy, new SettingRange(null, 7) },
            { Beatmap.Difficulty.Normal, new SettingRange(null, 7.5) },
            { Beatmap.Difficulty.Hard, new SettingRange(null, 8) },
        };

        protected override IReadOnlyList<SettingDefinition> Settings { get; } =
        [
            new SettingDefinition(
                "HP",
                beatmap => beatmap.DifficultySettings.hpDrain,
                HpDrainRanges
            ),
            new SettingDefinition(
                "OD",
                beatmap => beatmap.DifficultySettings.overallDifficulty,
                OverallDifficultyRanges
            ),
        ];

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

        protected override Issue BuildIssue(
            Beatmap beatmap,
            string setting,
            SettingRange range,
            Beatmap.Difficulty difficulty,
            double current
        )
        {
            var diffName = beatmap.MetadataSettings.version;
            var templateKey = setting == "HP" ? "HP Problem" : "OD Problem";

            return new Issue(GetTemplate(templateKey), beatmap, diffName, range.Max, current);
        }

        protected override IEnumerable<Issue> GetAdditionalIssues(
            Beatmap beatmap,
            Beatmap.Difficulty difficulty
        )
        {
            if (difficulty != Beatmap.Difficulty.Ultra)
                yield break;

            var diffName = beatmap.MetadataSettings.version;
            var hp = Round(beatmap.DifficultySettings.hpDrain);
            var od = Round(beatmap.DifficultySettings.overallDifficulty);

            yield return new Issue(GetTemplate("Ambiguous"), beatmap, diffName, hp, od);
        }
    }
}
