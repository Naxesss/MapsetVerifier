using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Checks.Utils.GeneralUtils;
using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class CheckSpinnerReadability : BeatmapCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);

        /// <summary>
        ///     Minimum gap before a spinner, in beats of <see cref="UninheritedLine.GetNormalizedMsPerBeat" />.
        /// </summary>
        private static readonly Dictionary<Beatmap.Difficulty, double> MinimalGapBeats = new()
        {
            { Beatmap.Difficulty.Easy, 1.0 / 2 },
            { Beatmap.Difficulty.Normal, 1.0 / 2 },
            { Beatmap.Difficulty.Hard, 1.0 / 2 },
            { Beatmap.Difficulty.Insane, 1.0 / 4 },
            { Beatmap.Difficulty.Expert, 1.0 / 4 },
        };

        private readonly Beatmap.Difficulty[] difficulties =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
        ];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Phob",
                Category = "Compose",
                Message = "Spinner readability",
                Difficulties = difficulties,
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Pointing out spinners that may be very close to their preceding object."
                    },
                    {
                        "Reasoning",
                        @"
                    Spinners can cause reading issues when being too close to their preceding object due to the visual overlap, especially in lower difficulties."
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
                        "{0} Note is very close to spinner. Ensure there are no readability issues.",
                        "timestamp -"
                    ).WithCause(
                        "The note is very close to the spinner, risking readability issues in certain cases."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var hitObjects = beatmap.HitObjects;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var current = hitObjects[i];
                var next = hitObjects.SafeGetIndex(i + 1);

                if (next is not Spinner)
                {
                    continue;
                }

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                if (timing == null)
                {
                    continue;
                }

                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();
                var currentEndTime = current is Slider slider ? slider.EndTime : current.time;
                var gap = next.time - currentEndTime;

                foreach (var diff in difficulties)
                {
                    var minimalGapMs = MinimalGapBeats[diff] * normalizedMsPerBeat;

                    if (gap < minimalGapMs)
                    {
                        yield return new Issue(
                            GetTemplate(Minor),
                            beatmap,
                            Timestamp.Get(current, next)
                        ).ForDifficulties(diff);
                    }
                }
            }
        }
    }
}
