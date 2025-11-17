using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.TaikoUtils;
using static MapsetVerifier.Checks.Utils.GeneralUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class CheckSpinnerReadability : BeatmapCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);

        private readonly Beatmap.Difficulty[] difficulties =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
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
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Minor,
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} Note is too close to spinner",
                        "timestamp - "
                    ).WithCause("The note is too close to the spinner")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var hitObjects = beatmap.HitObjects;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                var current = hitObjects[i];
                var next = hitObjects.SafeGetIndex(i + 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                if (timing == null)
                {
                    continue;
                }
                
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>();
                minimalGap.AddRange(difficulties.Take(3), normalizedMsPerBeat / 2);
                minimalGap.AddRange(difficulties.Skip(3).Take(3), normalizedMsPerBeat / 4);
                
                // We only have to check the note before a spinner, not after
                if (next is not Spinner)
                    continue;

                var currentEndTime = current is Slider slider ? slider.EndTime : current.time;
                var gap = next.time - currentEndTime;

                foreach (var diff in difficulties)
                {
                    if (gap < minimalGap[diff])
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
