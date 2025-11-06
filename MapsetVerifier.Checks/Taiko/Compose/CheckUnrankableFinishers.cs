using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class CheckUnrankableFinishers : BeatmapCheck
    {
        private const string Warning = nameof(Issue.Level.Warning);
        private const string Problem = nameof(Issue.Level.Problem);

        private const double HalfBeat = 1.0 / 2;
        private const double ThirdBeat = 1.0 / 3;
        private const double QuarterBeat = 1.0 / 4;
        private const double SixthBeat = 1.0 / 6;

        private readonly Beatmap.Difficulty[] difficulties =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
            Beatmap.Difficulty.Ultra
        ];

        // any finisher pattern spacing equal to or smaller than this gap is a problem
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeats = new()
        {
            { Beatmap.Difficulty.Easy, HalfBeat },
            { Beatmap.Difficulty.Normal, ThirdBeat },
            { Beatmap.Difficulty.Hard, QuarterBeat },
            { Beatmap.Difficulty.Insane, SixthBeat }
        };

        // any finisher pattern spacing equal to or smaller than this gap is a warning
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsWarning = new()
        {
            { Beatmap.Difficulty.Hard, ThirdBeat }
        };

        // any finisher pattern spacing equal to or smaller than this gap without a color change before is a problem
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsRequiringColorChangeBefore = new()
        {
            { Beatmap.Difficulty.Insane, QuarterBeat }
        };

        // any finisher pattern spacing equal to or smaller than this gap without a color change after is a problem
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsRequiringColorChangeAfter =
            new() { };

        // any finisher pattern spacing equal to or smaller than this gap without a color change before is a warning
        private static readonly Dictionary<Beatmap.Difficulty, double>
            MaximalGapBeatsRequiringColorChangeBeforeWarning = new()
            {
                { Beatmap.Difficulty.Expert, ThirdBeat },
                { Beatmap.Difficulty.Ultra, ThirdBeat }
            };

        // any finisher pattern spacing equal to or smaller than this gap without a color change after is a warning
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsRequiringColorChangeAfterWarning =
            new()
            {
                { Beatmap.Difficulty.Expert, ThirdBeat },
                { Beatmap.Difficulty.Ultra, ThirdBeat }
            };

        // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a problem
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsRequiringFinalNote = new()
        {
            { Beatmap.Difficulty.Insane, QuarterBeat }
        };

        // any finisher pattern spacing equal to or smaller than this gap while not being at the end of the pattern is a warning
        private static readonly Dictionary<Beatmap.Difficulty, double> MaximalGapBeatsRequiringFinalNoteWarning = new()
        {
            { Beatmap.Difficulty.Expert, ThirdBeat },
            { Beatmap.Difficulty.Ultra, ThirdBeat }
        };

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
                    if ((CheckGap(diff, MaximalGapBeats, beatmap, current) && isInPattern) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringColorChangeBefore, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringColorChangeAfter, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringFinalNote, beatmap, current) && !isFinalNote)) {
                        yield return new Issue(
                            GetTemplate(Problem),
                            beatmap,
                            Timestamp.Get(current.time)
                        ).ForDifficulties(diff);
                        continue;
                    }

                    // check for abnormal finishers (warning)
                    if ((CheckGap(diff, MaximalGapBeatsWarning, beatmap, current) && isInPattern) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringColorChangeBeforeWarning, beatmap, current) && sameColorBefore && !isFirstNote) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringColorChangeAfterWarning, beatmap, current) && sameColorAfter && !isFinalNote) ||
                        (CheckGap(diff, MaximalGapBeatsRequiringFinalNoteWarning, beatmap, current) && !isFinalNote)) {
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

            if (current.GetPatternSpacingMs() <= maximalGapMs + Common.MS_EPSILON)
            {
                return true;
            }

            return false;
        }
    }
}
