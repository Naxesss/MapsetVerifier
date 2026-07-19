using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Difficulty;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings
{
    [Check]
    public class CheckScoreOverflow : BeatmapCheck
    {
        private const int MaxStableCombo = ushort.MaxValue;
        private const long MaxStableScore = int.MaxValue;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Settings",
                Message = "Score/Combo overflow.",
                Author = "Hivie",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Warning when a difficulty's maximum ScoreV1 (HDHRDTFL SS) exceeds the signed 32-bit integer limit,
                        or when its maximum combo exceeds the 16-bit unsigned integer limit."
                    },
                    {
                        "Reasoning",
                        @"
                        Stable stores ScoreV1 as a signed 32-bit integer (max 2,147,483,647). If a perfect play
                        would exceed that, it would cause ScoreV2 mod to be automatically enabled for stable, therefore
                        rendering scores unsubmittable. This mostly happens on long, high-combo maps.

                        HD+HR+DT+FL is the highest ScoreV1 mod multiplier product available in osu!, osu!taiko, and osu!catch, so
                        it's used as a reference for the maximum achievable score.
                        osu!mania is excluded from the score check because its ScoreV1 is already capped at 1,000,000.

                        Stable also stores combo as a 16-bit unsigned integer (max 65,535). Maps whose maximum combo exceeds
                        that will overflow the combo counter. This applies to all game modes.

                        To address overflow issues, a 'lazer-only' flag should be added to the map, which users should request
                        the NAT to add prior to having the map nominated."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "ScoreOverflow",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Score overflows in osu!(stable) ({0}). Ensure the map has the 'lazer-only' flag.",
                        "overflowing score"
                    ).WithCause(
                        "The maximum ScoreV1 achievable with an HDHRDTFL SS exceeds the 32-bit integer limit (2,147,483,647)."
                    )
                },
                {
                    "ComboOverflow",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Combo overflows in osu!(stable) ({0}). Ensure the map has the 'lazer-only' flag.",
                        "max combo"
                    ).WithCause(
                        "The maximum achievable combo exceeds the 16-bit unsigned integer limit (65,535)."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.HitObjects.Count == 0)
                yield break;

            int maxCombo = beatmap.DifficultyAttributes?.MaxCombo ?? 0;

            if (maxCombo > MaxStableCombo)
            {
                yield return new Issue(
                    GetTemplate("ComboOverflow"),
                    beatmap,
                    maxCombo.ToString("N0", CultureInfo.InvariantCulture)
                );
            }

            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                yield break;

            long maxScore = LegacyScoreHelper.GetMaximumLegacyScore(beatmap);

            if (maxScore <= MaxStableScore)
                yield break;

            yield return new Issue(
                GetTemplate("ScoreOverflow"),
                beatmap,
                maxScore.ToString("N0", CultureInfo.InvariantCulture)
            );
        }
    }
}
