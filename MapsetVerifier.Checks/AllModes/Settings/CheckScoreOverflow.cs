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
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Taiko,
                    Beatmap.Mode.Catch,
                    // osu!mania ScoreV1 is capped at 1 million and cannot overflow
                ],
                Category = "Settings",
                Message = "Score overflow.",
                Author = "Hivie",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Warning when a difficulty's maximum ScoreV1 (HDHRDTFL SS) exceeds the signed 32-bit integer limit."
                    },
                    {
                        "Reasoning",
                        @"
                        Stable stores ScoreV1 as a signed 32-bit integer (max 2,147,483,647). If a perfect HD+HR+DT+FL play
                        would exceed that, it would cause ScoreV2 mod to be automatically enabled for stable, therefore
                        rendering the map unplayable. This mostly happens on long, high-combo maps.

                        HD+HR+DT+FL is the highest ScoreV1 mod multiplier product available in osu!, osu!taiko, and osu!catch.

                        To address this, a 'lazer-only' flag can be added to the map, which users can request the NAT to add
                        prior to having the map nominated.

                        osu!mania is excluded because its ScoreV1 is already capped at 1,000,000."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Overflow",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Score overflows in osu!(stable) ({0}). Ensure the map has the 'lazer-only' flag.",
                        "overflowing score"
                    ).WithCause(
                        "The maximum ScoreV1 achievable with an HDHRDTFL SS exceeds the 32-bit integer limit (2,147,483,647)."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.HitObjects.Count == 0)
                yield break;

            long maxScore = LegacyScoreHelper.GetMaximumLegacyScore(beatmap);

            if (maxScore <= int.MaxValue)
                yield break;

            yield return new Issue(
                GetTemplate("Overflow"),
                beatmap,
                maxScore.ToString("N0", CultureInfo.InvariantCulture)
            );
        }
    }
}
