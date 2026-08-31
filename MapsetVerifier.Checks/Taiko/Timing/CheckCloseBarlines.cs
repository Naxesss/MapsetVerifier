using MapsetVerifier.Checks.Utils;
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
    public class CheckCloseBarlines : BeatmapCheck
    {
        private const string Problem = nameof(Issue.Level.Problem);
        private const string Warning = nameof(Issue.Level.Warning);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie",
                Category = "Timing",
                Message = "Close barlines",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that a barline from the previous red line is not within one metronome of the next red line, with a problem when it is within half a metronome."
                    },
                    {
                        "Reasoning",
                        @"
                    Double barlines are visually disruptive and confusing in the representation of a song's downbeat. Incomplete measures before a new red line place a barline close to that red line's barline."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Problem,
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Barline is very close to the previous barline ({1}).",
                        "timestamp -",
                        "x/1"
                    ).WithCause(
                        "Red line is within half a metronome of a downbeat from the previous red line."
                    )
                },
                {
                    Warning,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Barline is close to the previous barline ({1}), ensure this makes sense.",
                        "timestamp -",
                        "x/1"
                    ).WithCause(
                        "Red line is within one metronome of a downbeat from the previous red line."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var redLines = beatmap.TimingLines.OfType<UninheritedLine>().ToList();

            for (int i = 0; i < redLines.Count; i++)
            {
                var current = redLines[i];
                var next = redLines.SafeGetIndex(i + 1);

                if (next == null)
                {
                    continue;
                }

                var barlineGap = current.msPerBeat * current.Meter;
                var halfMetronome = barlineGap / 2;
                var distance = next.Offset - current.Offset;

                // if the next line has an omit, double barlines can't happen
                // if the current line has an omit and lasts only 1 measure, double barlines can't happen either
                // true for not insanely high bpms, but who cares ^
                if (next.OmitsBarLine || (current.OmitsBarLine && distance <= barlineGap))
                    continue;

                var rest = distance % barlineGap;

                // Truncated BPM values (e.g. 260 BPM stored as 230.769230769231)
                // plus integer red-line offsets routinely land a fraction of a
                // millisecond short of a complete measure. The previous downbeat
                // is then a full measure away, not a close barline.
                if (barlineGap - rest <= Common.MS_EPSILON)
                    continue;

                if (rest > 0)
                {
                    var snap = TimingUtils.FormatClosestBeatSnap(rest, current.msPerBeat);

                    if (rest < 0.5)
                    {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(next.Offset),
                            snap
                        );
                    }
                    else if (rest <= halfMetronome)
                    {
                        yield return new Issue(
                            GetTemplate(Problem),
                            beatmap,
                            Timestamp.Get(next.Offset),
                            snap
                        );
                    }
                    else
                    {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(next.Offset),
                            snap
                        );
                    }
                }
            }
        }
    }
}
