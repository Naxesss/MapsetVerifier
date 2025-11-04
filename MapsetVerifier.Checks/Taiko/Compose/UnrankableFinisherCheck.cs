using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

using static MapsetVerifier.Checks.Utils.TaikoUtils;
using static MapsetVerifier.Checks.Utils.GeneralUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class UnrankableFinisherCheck : BeatmapCheck
    {
        private const string Warning = nameof(Issue.Level.Warning);
        private const string Problem = nameof(Issue.Level.Problem);
        
        private readonly Beatmap.Difficulty[] difficulties =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
            Beatmap.Difficulty.Ultra
        ];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Nostril",
                Category = "Compose",
                Message = "Unrankable finishers",
                Difficulties = difficulties,
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that finishers abide by each difficulty's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    Improper finisher usage can lead to significant gameplay issues."
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
                        "{0} Abnormal finisher, ensure this makes sense",
                        "timestamp - "
                    ).WithCause("Finisher is potentially violating the Ranking Criteria")
                },
                {
                    Problem,
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} Unrankable finisher",
                        "timestamp - "
                    ).WithCause("Finisher is violating the Ranking Criteria")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var finishers = beatmap.HitObjects
                .Where(x => x is Circle && x.IsFinisher())
                .ToList();

            // any finisher pattern spacing equal to or smaller than this gap is a problem
            var maximalGapBeats = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Easy,  1.0/2 },
                { Beatmap.Difficulty.Normal,  1.0/3 },
                { Beatmap.Difficulty.Hard,    1.0/4 },
                { Beatmap.Difficulty.Insane,     1.0/6 }
            };

            // any finisher pattern spacing equal to or smaller than this gap is a problem
            var maximalGapBeatsWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Hard,    1.0/3 }
            };

            // any finisher pattern spacing equal to or smaller than this gap without a color change before is a problem
            var maximalGapBeatsRequiringColorChangeBefore = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Insane,     1.0/4 }
            };

            // any finisher pattern spacing equal to or smaller than this gap without a color change after is a problem
            var maximalGapBeatsRequiringColorChangeAfter = new Dictionary<Beatmap.Difficulty, double>() { };

            // any finisher pattern spacing equal to or smaller than this gap without a color change before is a warning
            var maximalGapBeatsRequiringColorChangeBeforeWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Expert,   1.0/3 },
                { Beatmap.Difficulty.Ultra,    1.0/3 }
            };

            // any finisher pattern spacing equal to or smaller than this gap without a color change after is a warning
            var maximalGapBeatsRequiringColorChangeAfterWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Expert,   1.0/3 },
                { Beatmap.Difficulty.Ultra,    1.0/3 }
            };

            // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a problem
            var maximalGapBeatsRequiringFinalNote = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Insane,     1.0/4 }
            };

            // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a warning
            var maximalGapBeatsRequiringFinalNoteWarning = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Expert,   1.0/3 },
                { Beatmap.Difficulty.Ultra,    1.0/3 }
            };

            foreach (var diff in difficulties)
            {
                foreach (var current in finishers)
                {
                    var sameColorBefore = current.IsMono();
                    var sameColorAfter = current.Next()?.IsMono() ?? false;
                    var isInPattern = !current.IsNotInPattern();
                    var isFirstNote = current.IsAtBeginningOfPattern();
                    var isFinalNote = current.IsAtEndOfPattern();

                    // check for unrankable finishers (problem)
                    if ((CheckGap(diff, maximalGapBeats, beatmap, current) && isInPattern) ||
                        (CheckGap(diff, maximalGapBeatsRequiringColorChangeBefore, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (CheckGap(diff, maximalGapBeatsRequiringColorChangeAfter, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (CheckGap(diff, maximalGapBeatsRequiringFinalNote, beatmap, current) && !isFinalNote)) {
                        yield return new Issue(
                            GetTemplate(Problem),
                            beatmap,
                            Timestamp.Get(current.time)
                        ).ForDifficulties(diff);
                        continue;
                    }

                    // check for abnormal finishers (warning)
                    if ((CheckGap(diff, maximalGapBeatsWarning, beatmap, current) && isInPattern) ||
                        (CheckGap(diff, maximalGapBeatsRequiringColorChangeBeforeWarning, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (CheckGap(diff, maximalGapBeatsRequiringColorChangeAfterWarning, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (CheckGap(diff, maximalGapBeatsRequiringFinalNoteWarning, beatmap, current) && !isFinalNote)) {
                        yield return new Issue(
                            GetTemplate(Warning),
                            beatmap,
                            Timestamp.Get(current.time)
                        ).ForDifficulties(diff);
                        continue;
                    }
                }
            }
            yield break;
        }

        private bool CheckGap(
            Beatmap.Difficulty diff,
            Dictionary<Beatmap.Difficulty, double> maximalGapBeats,
            Beatmap beatmap,
            HitObject current)
        {
            // if check isn't relevant to this diff, don't check it
            if (!maximalGapBeats.ContainsKey(diff))
            {
                return false;
            }

            // convert maximal gap from # beats to milliseconds
            var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
            var maximalGapMs = maximalGapBeats[diff] * timing.GetNormalizedMsPerBeat();

            if (current.GetPatternSpacingMs() <= maximalGapMs + MS_EPSILON)
            {
                return true;
            }

            return false;
        }
    }
}
