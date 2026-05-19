using System.Globalization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckInvalidVolume : BeatmapCheck
    {
        private const float MinVolume = 5;
        private const float MaxVolume = 100;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Invalid timing line volume.",
                Author = "Hivie",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing timing lines with sample volumes outside the range supported by the osu! editor."
                    },
                    {
                        "Reasoning",
                        @"
                        Timing line volume is limited to 5–100% in the editor. Values below 5% or above 100%, such as 105%, 
                        are usually introduced by external hitsounding tools and can crash the osu! client when 
                        selecting the line in the timing panel.
                        "
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Invalid Volume",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Invalid timing line volume of {1}%.",
                        "timestamp -",
                        "volume"
                    ).WithCause("A timing line has a sample volume below 5% or above 100%.")
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var line in beatmap.TimingLines)
            {
                if (line.Volume is >= MinVolume and <= MaxVolume)
                    continue;

                yield return new Issue(
                    GetTemplate("Invalid Volume"),
                    beatmap,
                    Timestamp.Get(line.Offset),
                    line.Volume.ToString("0.##", CultureInfo.InvariantCulture)
                );
            }
        }
    }
}
