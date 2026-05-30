using System.Collections.Concurrent;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckWrongSnapping : BeatmapSetCheck
    {
        // How close two edges must be to count as the same object across difficulties.
        private const double EdgeMatchToleranceMs = 3;

        private readonly int[] divisors = [1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16];
        private HashSet<int> countMinorDivisors = [];
        private List<Tuple<int, string>> countMinorStamps = [];

        private HashSet<int> countWarningDivisors = [];

        private List<Tuple<int, string>> countWarningStamps = [];

        private ConcurrentBag<Inconsistency> inconsistencies = [];
        private HashSet<int> percentMinorDivisors = [];
        private List<Tuple<int, string>> percentMinorStamps = [];
        private HashSet<int> percentWarningDivisors = [];
        private List<Tuple<int, string>> percentWarningStamps = [];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Wrongly or inconsistently snapped hit objects.",
                Author = "Naxess, Hivie",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing incorrectly snapped hit objects, for example 1/6 being used where the song only supports 
                        1/4, or a slider tail accidentally being extended 1/16 too far."
                    },
                    {
                        "Reasoning",
                        @"
                        Should hit objects not align with any audio cue or otherwise recognizable pattern, it would not only 
                        force the player to guess when objects should be clicked, but also harm the perceived connection between 
                        the beatmap and the song, neither of which make for good experiences.
                        
                        > Note that this check is intentionally heavy on false-positives for safety's sake due to this being a common disqualification reason."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                // warnings
                {
                    "Snap Consistency",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} (1/{1}) Different snapping, {2} (1/{3}), is used in {4}. Ensure this makes sense.",
                        "timestamp -",
                        "X",
                        "timestamp -",
                        "X",
                        "difficulty"
                    ).WithCause(
                        @"Two hit objects in separate difficulties do not have any object in the other difficulty at the same time, and are close enough in time to be mistaken for one another.
                                    > Ignores cases where the divisor on the lower difficulty is less than on the higher difficulty, since this is usually natural.
                                    > Cross-family snaps (e.g. 1/4 vs 1/6) and exotic divisors (e.g. 1/16) are more severe than mismatches within the 1/2–1/4–1/8 or 1/3–1/6 families."
                    )
                },
                {
                    "Snap Count",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} 1/{1} is used 3 times or less, ensure this makes sense.",
                        "timestamp(s) -",
                        "X"
                    ).WithCause(
                        "The beat snap divisor a hit object is on is used less than or equal to 3 times in the same difficulty and is 1/6 or lower."
                    )
                },
                {
                    "Snap Percent",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} 1/{1} makes out 0.5% or less of snappings, ensure this makes sense.",
                        "timestamp(s) -",
                        "X"
                    ).WithCause(
                        "The beat snap divisor a hit object is on is used less than or equal to 0.5% of all snappings in the same difficulty and is 1/6 or lower."
                    )
                },
                // minors
                {
                    "Minor Snap Consistency",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} (1/{1}) Different snapping, {2} (1/{3}), is used in {4}. Ensure this makes sense.",
                        "timestamp -",
                        "X",
                        "timestamp -",
                        "X",
                        "difficulty"
                    ).WithCause(
                        @"Two hit objects in separate difficulties do not have any object in the other difficulty at the same time, and are close enough in time to be mistaken for one another, but both use divisors from the same common family (1/2–1/4–1/8 or 1/3–1/6).
                                    > Ignores cases where the divisor on the lower difficulty is less than on the higher difficulty, since this is usually natural."
                    )
                },
                {
                    "Minor Snap Count",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} 1/{1} is used 7 times or less, ensure this makes sense.",
                        "timestamp(s) -",
                        "X"
                    ).WithCause("Same as the other check, except with 7 as threshold instead.")
                },
                {
                    "Minor Snap Percent",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} 1/{1} makes out 5% or less of snappings, ensure this makes sense.",
                        "timestamp(s) -",
                        "X"
                    ).WithCause("Same as the other check, except with 5% as threshold instead.")
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                inconsistencies = [];

                // Phase 1: compare this diff against higher-SR diffs for mismatched snapping nearby in time.
                // Essentially if our `otherBeatmaps` simplify rhythms from our `beatmap`, then that's an issue.
                // So the reason `otherBeatmaps` is all higher difficulties is because lower difficulties are expected to simplify rhythms.
                var otherBeatmaps = beatmapSet.Beatmaps.Where(otherBeatmap =>
                    otherBeatmap.StarRating > beatmap.StarRating
                    && otherBeatmap.GeneralSettings.mode == beatmap.GeneralSettings.mode
                );

                Parallel.ForEach(
                    otherBeatmaps,
                    otherBeatmap => PopulateInconsistencies(beatmap, otherBeatmap)
                );

                foreach (
                    var inconsistentEdgeTime in inconsistencies
                        .Select(inconsistency => inconsistency.inconsistentEdgeTime)
                        .Distinct()
                )
                {
                    var respectiveBeatmap = inconsistencies
                        .Where(inconsistency =>
                            inconsistency.inconsistentEdgeTime.AlmostEqual(inconsistentEdgeTime)
                        )
                        .Select(inconsistency => inconsistency.respectiveBeatmap)
                        .First();

                    var respectiveEdgeTime = inconsistencies
                        .Where(inconsistency =>
                            inconsistency.inconsistentEdgeTime.AlmostEqual(inconsistentEdgeTime)
                        )
                        .Select(inconsistency => inconsistency.respectiveEdgeTime)
                        .FirstOrDefault();

                    // Each edge belongs to its own difficulty; divisors must come from that beatmap's timing.
                    var divisorThis = beatmap.GetLowestDivisor(inconsistentEdgeTime);
                    var divisorOther = respectiveBeatmap.GetLowestDivisor(respectiveEdgeTime);

                    if (divisorThis == 0 || divisorOther == 0)
                        continue;

                    var templateKey = IsMinorSnapInconsistency(divisorThis, divisorOther)
                        ? "Minor Snap Consistency"
                        : "Snap Consistency";

                    yield return new Issue(
                        GetTemplate(templateKey),
                        beatmap,
                        Timestamp.Get(inconsistentEdgeTime),
                        divisorThis,
                        Timestamp.Get(respectiveEdgeTime),
                        divisorOther,
                        respectiveBeatmap
                    );
                }

                // Phase 2: within this diff alone, flag snap divisors that appear rarely (count or % thresholds).
                AnalyzeRareDivisors(beatmap);

                foreach (var issue in GetDivisorIssues(beatmap, countWarningStamps, "Snap Count"))
                    yield return issue;

                foreach (
                    var issue in GetDivisorIssues(beatmap, percentWarningStamps, "Snap Percent")
                )
                    yield return issue;

                foreach (
                    var issue in GetDivisorIssues(beatmap, countMinorStamps, "Minor Snap Count")
                )
                    yield return issue;

                foreach (
                    var issue in GetDivisorIssues(beatmap, percentMinorStamps, "Minor Snap Percent")
                )
                    yield return issue;
            }
        }

        /// <summary>
        ///     Single pass over all object edges: tally divisor usage, then stamp times for rare ones.
        /// </summary>
        private void AnalyzeRareDivisors(Beatmap beatmap)
        {
            const int countWarning = 3;
            const int countMinor = 7;
            const double percentWarning = 0.005;
            const double percentMinor = 0.05;

            countWarningDivisors = [];
            countMinorDivisors = [];
            percentWarningDivisors = [];
            percentMinorDivisors = [];
            countWarningStamps = [];
            countMinorStamps = [];
            percentWarningStamps = [];
            percentMinorStamps = [];

            var divisorCounts = new Dictionary<int, int>();
            var edgeEntries = new List<(double edgeTime, int divisor, bool isUnsnapped)>();

            // Cache divisor/unsnap per edge so we don't re-query timing for the stamp pass below.
            foreach (var hitObject in beatmap.HitObjects)
            foreach (var edgeTime in hitObject.GetEdgeTimes())
            {
                var isUnsnapped = beatmap.GetUnsnapIssue(edgeTime) != null;
                var divisor = beatmap.GetLowestDivisor(edgeTime);
                edgeEntries.Add((edgeTime, divisor, isUnsnapped));

                if (divisor != 0)
                    divisorCounts[divisor] = divisorCounts.GetValueOrDefault(divisor) + 1;
            }

            var divisorsTotal = divisorCounts.Values.Sum();

            foreach (var (divisor, count) in divisorCounts)
            {
                var precentage = count / (double)divisorsTotal;

                if (count <= countWarning)
                    countWarningDivisors.Add(divisor);
                else if (count <= countMinor)
                    countMinorDivisors.Add(divisor);

                if (precentage < percentWarning)
                    percentWarningDivisors.Add(divisor);
                else if (precentage < percentMinor)
                    percentMinorDivisors.Add(divisor);
            }

            foreach (var (edgeTime, divisor, isUnsnapped) in edgeEntries)
            {
                if (isUnsnapped || divisor == 0)
                    continue;

                if (countWarningDivisors.Contains(divisor))
                    countWarningStamps.Add(
                        new Tuple<int, string>(divisor, Timestamp.Get(edgeTime))
                    );
                else if (percentWarningDivisors.Contains(divisor))
                    percentWarningStamps.Add(
                        new Tuple<int, string>(divisor, Timestamp.Get(edgeTime))
                    );
                else if (countMinorDivisors.Contains(divisor))
                    countMinorStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(edgeTime)));
                else if (percentMinorDivisors.Contains(divisor))
                    percentMinorStamps.Add(
                        new Tuple<int, string>(divisor, Timestamp.Get(edgeTime))
                    );
            }

            countWarningStamps = countWarningStamps.Distinct().ToList();
            countMinorStamps = countMinorStamps.Distinct().ToList();
            percentWarningStamps = percentWarningStamps.Distinct().ToList();
            percentMinorStamps = percentMinorStamps.Distinct().ToList();
        }

        /// <summary>
        ///     Supplied with the divisor issue list, this function basically just turns them into readable issues
        ///     which we can then return in GetIssues.
        /// </summary>
        private IEnumerable<Issue> GetDivisorIssues(
            Beatmap beatmap,
            IReadOnlyCollection<Tuple<int, string>> divisorTupleList,
            string templateKey
        )
        {
            if (divisorTupleList.Count == 0)
                yield break;

            foreach (var divisor in divisorTupleList.Select(stamp => stamp.Item1).Distinct())
            {
                var stamps = divisorTupleList
                    .Where(stamp => stamp.Item1 == divisor)
                    .Select(stamp => stamp.Item2);

                yield return new Issue(
                    GetTemplate(templateKey),
                    beatmap,
                    string.Join(" ", stamps),
                    divisor
                );
            }
        }

        /// <summary>
        ///     Populates the inconsistent places list, which keeps track of any
        ///     time values in either beatmap that has no corresponding value in the other.
        /// </summary>
        private void PopulateInconsistencies(Beatmap beatmap, Beatmap otherBeatmap)
        {
            foreach (var inconsistency in GetInconsistencies(beatmap, otherBeatmap))
                inconsistencies.Add(inconsistency);
        }

        internal static List<double> CollectEdgeTimes(Beatmap beatmap)
        {
            var edgeTimes = new List<double>();

            foreach (var hitObject in beatmap.HitObjects)
                edgeTimes.AddRange(hitObject.GetEdgeTimes());

            return edgeTimes;
        }

        internal static bool HasMatchingEdgeWithin(
            IReadOnlyList<double> sortedOtherEdgeTimes,
            double edgeTime
        )
        {
            // `sortedOtherEdgeTimes` must be sorted; we only scan the narrow ±3 ms window around `edgeTime`.
            var lowerBound = edgeTime - EdgeMatchToleranceMs;
            var upperBound = edgeTime + EdgeMatchToleranceMs;

            var start = LowerBound(sortedOtherEdgeTimes, lowerBound);

            for (var index = start; index < sortedOtherEdgeTimes.Count; index++)
            {
                var otherEdgeTime = sortedOtherEdgeTimes[index];
                if (otherEdgeTime >= upperBound)
                    break;

                if (Math.Abs(otherEdgeTime - edgeTime) < EdgeMatchToleranceMs)
                    return true;
            }

            for (var index = start - 1; index >= 0; index--)
            {
                var otherEdgeTime = sortedOtherEdgeTimes[index];
                if (otherEdgeTime <= lowerBound)
                    break;

                if (Math.Abs(otherEdgeTime - edgeTime) < EdgeMatchToleranceMs)
                    return true;
            }

            return false;
        }

        internal static List<double> GetMissingEdgeTimes(
            IReadOnlyList<double> edgeTimes,
            IReadOnlyList<double> sortedOtherEdgeTimes
        )
        {
            var missingEdgeTimes = new List<double>();

            foreach (var edgeTime in edgeTimes)
                if (!HasMatchingEdgeWithin(sortedOtherEdgeTimes, edgeTime))
                    missingEdgeTimes.Add(edgeTime);

            return missingEdgeTimes;
        }

        private static int LowerBound(IReadOnlyList<double> values, double target)
        {
            var low = 0;
            var high = values.Count;

            while (low < high)
            {
                var mid = low + (high - low) / 2;

                if (values[mid] < target)
                    low = mid + 1;
                else
                    high = mid;
            }

            return low;
        }

        private IEnumerable<Inconsistency> GetInconsistencies(Beatmap beatmap, Beatmap otherBeatmap)
        {
            var edgeTimes = CollectEdgeTimes(beatmap);
            var otherEdgeTimes = CollectEdgeTimes(otherBeatmap);
            var sortedOtherEdgeTimes = otherEdgeTimes.OrderBy(time => time).ToList();
            var sortedEdgeTimes = edgeTimes.OrderBy(time => time).ToList();

            // Edges with no counterpart within 3 ms in the other diff — computed once per direction.
            var missingEdgeTimes = GetMissingEdgeTimes(edgeTimes, sortedOtherEdgeTimes);
            var otherMissingEdgeTimes = GetMissingEdgeTimes(otherEdgeTimes, sortedEdgeTimes);

            // Pair up nearby missing edges that could be mistaken for each other (see GetConsistencyRange).
            foreach (var missingEdgeTime in missingEdgeTimes)
            foreach (var otherMissingEdgeTime in otherMissingEdgeTimes)
            {
                var timeDifference = Math.Abs(missingEdgeTime - otherMissingEdgeTime);

                if (timeDifference <= EdgeMatchToleranceMs)
                    // If both maps somehow claim they have an object the other does not at the same time, we skip that case.
                    continue;

                var line = otherBeatmap.GetTimingLine<UninheritedLine>(missingEdgeTime);
                if (line == null)
                {
                    continue;
                }

                var msPerBeat = line.msPerBeat;

                if (timeDifference >= msPerBeat)
                    // Edges a beat apart or more should not be flagged as inconsistent, so skip those cases.
                    continue;

                var consistencyRange = GetConsistencyRange(
                    otherBeatmap,
                    missingEdgeTime,
                    msPerBeat,
                    otherMissingEdgeTime
                );

                if (
                    missingEdgeTime + consistencyRange > otherMissingEdgeTime
                    && missingEdgeTime - consistencyRange < otherMissingEdgeTime
                )
                    yield return new Inconsistency(
                        missingEdgeTime,
                        otherMissingEdgeTime,
                        otherBeatmap
                    );
            }
        }

        /// <summary>
        ///     Gets a time offset from a given divisor which may be confused with it. Larger for smaller divisors.
        ///     So the offset for a 1/1 would be larger than for a 1/6, for example. If two times are supplied, the largest divisor
        ///     is used.
        /// </summary>
        private double GetConsistencyRange(
            Beatmap otherBeatmap,
            double time,
            double msPerBeat,
            double? otherTime = null
        )
        {
            var divisor = Math.Max(otherBeatmap.GetLowestDivisor(time), 2);

            if (otherTime == null)
            {
                var divisorIndex = Array.IndexOf(divisors, divisor);

                var greaterDivisor = divisors.ElementAt(
                    divisorIndex + 2 > divisors.Length - 1 ? divisors.Length - 1 : divisorIndex + 2
                );

                const int unsnapMargin = 2;

                return msPerBeat / greaterDivisor - unsnapMargin;
            }

            var higherDiffDivisor = Math.Max(
                otherBeatmap.GetLowestDivisor(otherTime.GetValueOrDefault()),
                2
            );

            // If the higher difficulty uses higher snaps, that's assumed to be normal progression,
            // unless we go from 1/3 to 1/4 or similar, which would be pretty odd.
            if (divisor < higherDiffDivisor || (divisor % 3 != 0 && higherDiffDivisor % 3 == 0))
                return Math.Max(
                    GetConsistencyRange(otherBeatmap, time, msPerBeat),
                    GetConsistencyRange(otherBeatmap, otherTime.GetValueOrDefault(), msPerBeat)
                );

            return 2;
        }

        private static readonly HashSet<int> PowerOfTwoSnapGroup = [2, 4, 8];
        private static readonly HashSet<int> TripleSnapGroup = [3, 6];

        internal static bool IsMinorSnapInconsistency(int divisorThis, int divisorOther)
        {
            if (
                PowerOfTwoSnapGroup.Contains(divisorThis)
                && PowerOfTwoSnapGroup.Contains(divisorOther)
            )
                return true;

            if (TripleSnapGroup.Contains(divisorThis) && TripleSnapGroup.Contains(divisorOther))
                return true;

            return false;
        }

        private readonly struct Inconsistency
        {
            public readonly double inconsistentEdgeTime;
            public readonly double respectiveEdgeTime;
            public readonly Beatmap respectiveBeatmap;

            public Inconsistency(
                double inconsistentEdgeTime,
                double respectiveEdgeTime,
                Beatmap respectiveBeatmap
            )
            {
                this.inconsistentEdgeTime = inconsistentEdgeTime;
                this.respectiveEdgeTime = respectiveEdgeTime;
                this.respectiveBeatmap = respectiveBeatmap;
            }
        }
    }
}
