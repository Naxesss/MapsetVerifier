using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Checks.Utils.GeneralUtils;
using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckKiaiFlash : BeatmapCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);
        private const string Warning = nameof(Issue.Level.Warning);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie",
                Category = "Timing",
                Message = "Kiai flashes",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that kiai flash usage is not too drastic.

                    **Thresholds**:

                    | Severity | Maximum flash length |
                    | :-: | :-- |
                    | **Warning** | ≤ 1/3 beat |
                    | **Minor** | ≤ 1/2 beat, but longer than the warning threshold |

                    Flashes longer than 1/2 beat are not flagged."
                    },
                    {
                        "Reasoning",
                        @"
                    Kiai flashes in osu!taiko cause the entire playfield to flash, which can cause performance problems, alongside potentially causing epileptic effects if abused."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Minor,
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} Kiai flash (1/{1}).",
                        "timestamp -",
                        "X"
                    ).WithCause("A kiai flash exists, but is not too drastic")
                },
                {
                    Warning,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Kiai flash (1/{1}).",
                        "timestamp -",
                        "X"
                    ).WithCause("A kiai flash that's too drastic exists")
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var kiaiToggles = beatmap.GetKiaiToggles();

            foreach (var toggle in kiaiToggles)
            {
                int currentIndex = kiaiToggles.IndexOf(toggle);
                int nextIndex = currentIndex + 1;

                if (nextIndex < kiaiToggles.Count)
                {
                    var timing = beatmap.GetTimingLine<UninheritedLine>(toggle.Offset);
                    if (timing == null)
                    {
                        continue;
                    }

                    var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();
                    var kiaiToggle = kiaiToggles[nextIndex];
                    double gap = kiaiToggle.Offset - toggle.Offset;
                    var divisor = TimingUtils.GetClosestSnapDivisor(gap, normalizedMsPerBeat);

                    if (gap <= Math.Ceiling(normalizedMsPerBeat / 3))
                    {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(toggle.Offset),
                            divisor
                        );
                    }
                    else if (gap <= Math.Ceiling(normalizedMsPerBeat / 2))
                    {
                        yield return new Issue(
                            GetTemplate(Minor),
                            beatmap,
                            Timestamp.Get(toggle.Offset),
                            divisor
                        );
                    }
                }
            }
        }
    }
}
