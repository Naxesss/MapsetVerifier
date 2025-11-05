using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckBarlineUnaffectedBySv : BeatmapCheck
    {
        private const double ThresholdMs = 5;
        private const string Warning = nameof(Issue.Level.Warning);
        private const string RoundingErrorWarning = "roundingErrorWarning";

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Timing",
                Message = "Barline is unaffected by a line very close to it.",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Preventing unintentional barline slider velocities caused by timing lines being slightly unsnapped."
                    },
                    {
                        "Reasoning",
                        @"
                    Barlines before a timing line (even if just by 1 ms or less), will not be affected by its slider velocity. With 1 ms unsnaps being common due to rounding errors when copy-pasting, this in turn becomes a common issue."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Warning,

                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Barline is snapped {1} ms before a line which would modify its slider velocity.",
                        "timestamp - ",
                        "unsnap"
                    ).WithCause("The spinner/slider end is unsnapped 1ms early.")
                },
                {
                    RoundingErrorWarning,

                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Barline may not have slider velocity properly applied due to rounding error. Double-check manually.",
                        "timestamp - "
                    ).WithCause("Rounding error.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var svChange in beatmap.FindSvChanges())
            {
                var unsnapMs = GetOffsetFromNearestBarlineMs(beatmap, svChange.Offset);
                if (unsnapMs < 0d && unsnapMs > -Common.ROUNDING_ERROR_MARGIN)
                {
                    yield return new Issue(
                       GetTemplate(RoundingErrorWarning),
                       beatmap,
                       Timestamp.Get(svChange.Offset - unsnapMs)
                   );
                }
                else if (unsnapMs <= ThresholdMs && unsnapMs > 0d)
                {
                    yield return new Issue(
                       GetTemplate(Warning),
                       beatmap,
                       Timestamp.Get(svChange.Offset - unsnapMs),
                       $"{unsnapMs:0.##}"
                   );
                }
            }
        }
    }
}
