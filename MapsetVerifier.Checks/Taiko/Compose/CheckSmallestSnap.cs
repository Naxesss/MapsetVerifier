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
    public class CheckSmallestSnap : BeatmapCheck
    {
        private const string Warning = nameof(Issue.Level.Warning);

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
                Message = "Unrankable snapping",
                Difficulties = difficulties,
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Preventing patterns with abnormally small snaps based on each difficulty's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    Certain snaps are too difficult/unreasonable for certain difficulties."
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
                        "{0} abnormally small gap, ensure it makes sense",
                        "timestamp - "
                    ).WithCause("Gap between notes may be too small")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var circles = beatmap.HitObjects.Where(x => x is Circle).ToList();

            // for each diff: var violatingGroup = new List<HitObject>();
            // lambda is used, because bare "new List<HitObject>()" would set the same instance in each pair
            var violatingGroup = new Dictionary<Beatmap.Difficulty, List<HitObject>>();
            violatingGroup.AddRange(difficulties, () => new List<HitObject>());

            for (int i = 0; i < circles.Count; i++)
            {
                var current = circles[i];
                var next = circles.SafeGetIndex(i + 1);

                var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                // for each diff: double minimalGap = ?;
                var minimalGap = new Dictionary<Beatmap.Difficulty, double>()
                {
                    { Beatmap.Difficulty.Easy, normalizedMsPerBeat / 2 },
                    { Beatmap.Difficulty.Normal, normalizedMsPerBeat / 3 },
                    { Beatmap.Difficulty.Hard, normalizedMsPerBeat / 6 },
                    { Beatmap.Difficulty.Insane, normalizedMsPerBeat / 6 }
                };

                var gap = (next?.time ?? double.MaxValue) - current.time;

                // for each diff: bool violatingGroupEnded = false;
                var violatingGroupEnded = new Dictionary<Beatmap.Difficulty, bool>();
                violatingGroupEnded.AddRange(difficulties, false);

                foreach (var diff in difficulties)
                {
                    CheckAndHandleIssues(
                        diff,
                        minimalGap,
                        violatingGroup,
                        violatingGroupEnded,
                        current,
                        next,
                        gap
                    );

                    if (violatingGroupEnded[diff])
                    {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(violatingGroup[diff].ToArray())
                        ).ForDifficulties(diff);

                        violatingGroup[diff].Clear();
                    }
                }
            }
        }

        private static void CheckAndHandleIssues(
            Beatmap.Difficulty diff,
            Dictionary<Beatmap.Difficulty, double> minimalGap,
            Dictionary<Beatmap.Difficulty, List<HitObject>> violatingGroup,
            Dictionary<Beatmap.Difficulty, bool> violatingGroupEnded,
            HitObject current,
            HitObject next,
            double gap
        )
        {
            if (gap < minimalGap[diff])
            {
                // add both the current and the next object only at the beginning of a group, add only the next one otherwise
                if (violatingGroup[diff].Count == 0)
                    violatingGroup[diff].Add(current);

                violatingGroup[diff].Add(next);
            }
            else if (violatingGroup[diff].Count != 0)
            {
                violatingGroupEnded[diff] = true;
            }
        }
    }
}
