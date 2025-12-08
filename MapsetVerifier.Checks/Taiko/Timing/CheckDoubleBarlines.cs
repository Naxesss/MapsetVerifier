using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.GeneralUtils;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckDoubleBarlines : BeatmapCheck
    {
        private const string Problem = nameof(Issue.Level.Problem);
        private const string Warning = nameof(Issue.Level.Warning);
        private const string RoundingErrorWarning = "roundingErrorWarning";

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Phob",
                Category = "Timing",
                Message = "Double barlines",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that there are no two barlines within 50ms of each other."
                    },
                    {
                        "Reasoning",
                        @"
                    Double barlines are caused by rounding errors. They are visually disruptive and confusing in the representation of a song's downbeat."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Problem,
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Double barline",
                        "timestamp -"
                    ).WithCause(
                        "Red line is extremely close to a downbeat from the previous red line"
                    )
                },
                {
                    Warning,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Potential double barline, doublecheck manually",
                        "timestamp -"
                    ).WithCause(
                        "Red line is extremely close to a downbeat from the previous red line"
                    )
                },
                {
                    RoundingErrorWarning,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Potential double barline due to rounding error, doublecheck manually",
                        "timestamp -"
                    ).WithCause(
                        "Rounding error"
                    )
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            const double threshold = 50;

            var redLines = beatmap.TimingLines
                .OfType<UninheritedLine>()
                .ToList();

            for (int i = 0; i < redLines.Count; i++)
            {
                var current = redLines[i];
                var next = redLines.SafeGetIndex(i + 1);

                var barlineGap = current.msPerBeat * current.Meter;
                var distance = (next?.Offset ?? double.MaxValue) - current.Offset;

                // if the next line has an omit, double barlines can't happen
                // if the current line has an omit and lasts only 1 measure, double barlines can't happen either
                // true for not insanely high bpms, but who cares ^
                if (
                    next == null
                    || next.OmitsBarLine
                    || (current.OmitsBarLine && distance <= barlineGap)
                )
                    continue;

                var rest = distance % barlineGap;

                if (rest - barlineGap > -Common.ROUNDING_ERROR_MARGIN)
                {
                    yield return new Issue(
                        GetTemplate(RoundingErrorWarning),
                        beatmap,
                        Timestamp.Get(next.Offset)
                    );
                }
                else if (rest - threshold <= 0 && rest > 0)
                {
                    if (rest >= 0.5)
                    {
                        yield return new Issue(
                            GetTemplate(Problem),
                            beatmap,
                            Timestamp.Get(next.Offset)
                        );
                    }
                    else
                    {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(next.Offset)
                        );
                    }
                }
            }
        }
    }
}
