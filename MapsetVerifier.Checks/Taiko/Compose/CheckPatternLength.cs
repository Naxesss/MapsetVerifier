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
    public class CheckPatternLength : BeatmapSetCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);
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
                Author = "SN707",
                Category = "Compose",
                Message = "Pattern Lengths",
                Difficulties = difficulties,
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Preventing patterns from being too long based on each difficulty's Ranking Criteria."
                    },
                    {
                        "Reasoning",
                        @"
                    On lower difficulties, patterns that use smaller snaps should be avoided as they can become straining if they exceed an unreasonable length."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() => new()
        {
            {
                Minor,

                new IssueTemplate(Issue.Level.Minor,
                    "{0} {1} {2} pattern is {3} notes long.",
                    "start", "end", "snap", "number")
                .WithCause("Pattern length is equal to the RC guideline")
            },


            {
                Warning,
                new IssueTemplate(Issue.Level.Warning,
                    "{0} {1} {2} pattern is {3} notes long, ensure this makes sense.",
                    "start", "end", "snap", "number")
                .WithCause("Pattern length is surpassing the RC guideline.")
            }
        };

        private static bool HasKantan(BeatmapSet beatmapSet)
        {
            return beatmapSet.Beatmaps.Any(b => b.GetDifficulty() == Beatmap.Difficulty.Easy);
        }

        /// <summary>
        ///     Returns a dictionary of difficulty, [snap size, snap count] pairs.
        /// </summary>
        private static Dictionary<Beatmap.Difficulty, Dictionary<double, int>> GetShortSnapParams(BeatmapSet beatmapSet)
        {
            var hasKantan = HasKantan(beatmapSet);

            return new Dictionary<Beatmap.Difficulty, Dictionary<double, int>>()
            {
                { Beatmap.Difficulty.Easy, new Dictionary<double, int>() {
                    { 1.0 / 1, 7 },
                    { 1.0 / 2, 2 }
                }},
                { Beatmap.Difficulty.Normal, new Dictionary<double, int>() {
                    { 1.0 / 2, hasKantan ? 7 : 5 },
                    { 1.0 / 3, 2 }
                }},
                { Beatmap.Difficulty.Hard, new Dictionary<double, int>() {
                    { 1.0 / 4, 5 },
                    { 1.0 / 6, 4 },
                }},
                { Beatmap.Difficulty.Insane, new Dictionary<double, int>() {
                    { 1.0 / 4, 9 },
                    { 1.0 / 6, 4 },
                    { 1.0 / 8, 2 },
                }},
            };
        }

        /// <summary>
        ///     Displays the fractions for output string.
        /// </summary>
        private static readonly Dictionary<double, string> OutputDict = new()
        {
            { 1.0 / 1, "1/1" },
            { 1.0 / 2, "1/2" },
            { 1.0 / 3, "1/3" },
            { 1.0 / 4, "1/4" },
            { 1.0 / 6, "1/6" },
            { 1.0 / 8, "1/8" }
        };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var shortSnapParams = GetShortSnapParams(beatmapSet);

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var objects = beatmap.HitObjects.Where(x => x is Circle).ToList();

                foreach (var diff in difficulties)
                {
                    foreach (var snapValues in shortSnapParams[diff])
                    {
                        var currentPatternStartTimeMs = objects.FirstOrDefault()?.time ?? 0;
                        var currentPatternEndTimeMs = objects.FirstOrDefault()?.time ?? 0;

                        // variables to identify length of pattern
                        var patternStartIndex = 0;
                        var foundStartOfPattern = false;
                        var foundEndOfPattern = false;
                        for (int i = 0; i < objects.Count; i++)
                        {
                            var current = objects.SafeGetIndex(i);
                            var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                            if (timing == null)
                            {
                                break;
                            }
                            
                            var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                            // convert minimal gap beats to milliseconds
                            var snapMs = snapValues.Key * normalizedMsPerBeat;


                            // check if this is end of pattern
                            if (i + 1 < objects.Count && foundStartOfPattern)
                            {
                                var gapBeginObject = objects.SafeGetIndex(i);
                                var gapEndObject = objects.SafeGetIndex(i + 1);

                                // Check if gap is greater than the snap size
                                var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                                if (gap - Common.MS_EPSILON > snapMs)
                                {
                                    foundEndOfPattern = true;
                                    currentPatternEndTimeMs = gapBeginObject.time;
                                }
                            } else if (i == objects.Count - 1 && foundStartOfPattern)
                            {
                                // last note, so forced end of pattern
                                foundEndOfPattern = true;
                                currentPatternEndTimeMs = objects.SafeGetIndex(i).time;
                            }

                            // check if this is start of pattern
                            if (i + 1 < objects.Count && !foundStartOfPattern)
                            {
                                var gapBeginObject = objects.SafeGetIndex(i);
                                var gapEndObject = objects.SafeGetIndex(i + 1);

                                // Check if gap is smaller than or equal to the snap size
                                var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                                if (gap - Common.MS_EPSILON <= snapMs)
                                {
                                    foundStartOfPattern = true;
                                    currentPatternStartTimeMs = gapBeginObject.time;
                                    patternStartIndex = i;
                                }
                            }

                            // check if this is the last note of a pattern, and if so check if it's too long
                            if (foundEndOfPattern && foundStartOfPattern)
                            {
                                foundEndOfPattern = false;
                                foundStartOfPattern = false; // resume checking for start of pattern
                                var durationOfPattern = i - patternStartIndex + 1;
                                
                                if (durationOfPattern > snapValues.Value)
                                {
                                    yield return new Issue(
                                        GetTemplate(Warning),
                                        beatmap,
                                        Timestamp.Get(currentPatternStartTimeMs).Trim() + ">",
                                        Timestamp.Get(currentPatternEndTimeMs).Trim(),
                                        OutputDict[snapValues.Key] ?? "unknown snap",
                                        durationOfPattern
                                    ).ForDifficulties(diff);
                                } else if (durationOfPattern == snapValues.Value)
                                {
                                    yield return new Issue(
                                        GetTemplate(Minor),
                                        beatmap,
                                        Timestamp.Get(currentPatternStartTimeMs).Trim() + ">",
                                        Timestamp.Get(currentPatternEndTimeMs).Trim(),
                                        OutputDict[snapValues.Key] ?? "unknown snap",
                                        durationOfPattern
                                    ).ForDifficulties(diff);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
