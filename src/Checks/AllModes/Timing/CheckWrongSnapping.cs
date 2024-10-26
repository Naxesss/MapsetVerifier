using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly int[] divisors = [1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16];
        private List<int> countMinorDivisors;
        private List<Tuple<int, string>> countMinorStamps;

        private List<int> countWarningDivisors;

        private List<Tuple<int, string>> countWarningStamps;

        private ConcurrentBag<Inconsistency> inconsistencies;
        private List<int> percentMinorDivisors;
        private List<Tuple<int, string>> percentMinorStamps;
        private List<int> percentWarningDivisors;
        private List<Tuple<int, string>> percentWarningStamps;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Wrongly or inconsistently snapped hit objects.",
                Author = "Naxess",

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
                    <note>
                        Note that this check is intentionally heavy on false-positives for safety's sake due to this being a 
                        common disqualification reason.
                    </note>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                // warnings
                {
                    "Snap Consistency",
                    new IssueTemplate(Issue.Level.Warning, "{0} (1/{1}) Different snapping, {2} (1/{3}), is used in {4}.", "timestamp - ", "X", "timestamp - ", "X", "difficulty").WithCause("Two hit objects in separate difficulties do not have any object in the other difficulty at the same time, " + "and are close enough in time to be mistaken for one another." + "<note>Ignores cases where the divisor on the lower difficulty is less than on the higher difficulty, since " + "this is usually natural.</note>")
                },

                {
                    "Snap Count",
                    new IssueTemplate(Issue.Level.Warning, "{0} 1/{1} is used 3 times or less, ensure this makes sense.", "timestamp(s) -", "X").WithCause("The beat snap divisor a hit object is on is used less than or equal to 3 times in the same difficulty " + "and is 1/6 or lower.")
                },

                {
                    "Snap Percent",
                    new IssueTemplate(Issue.Level.Warning, "{0} 1/{1} makes out 0.5% or less of snappings, ensure this makes sense.", "timestamp(s) -", "X").WithCause("The beat snap divisor a hit object is on is used less than or equal to 0.5% of all snappings in the same " + "difficulty and is 1/6 or lower.")
                },

                // minors
                {
                    "Minor Snap Count",
                    new IssueTemplate(Issue.Level.Minor, "{0} 1/{1} is used 7 times or less, ensure this makes sense.", "timestamp(s) -", "X").WithCause("Same as the other check, except with 7 as threshold instead.")
                },

                {
                    "Minor Snap Percent",
                    new IssueTemplate(Issue.Level.Minor, "{0} 1/{1} makes out 5% or less of snappings, ensure this makes sense.", "timestamp(s) -", "X").WithCause("Same as the other check, except with 5% as threshold instead.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                inconsistencies = [];

                // Essentially if our `otherBeatmaps` simplify rhythms from our `beatmap`, then that's an issue.
                // So the reason `otherBeatmaps` is all higher difficulties is because lower difficulties are expected to simplify rhythms.
                var otherBeatmaps = beatmapSet.Beatmaps.Where(otherBeatmap => otherBeatmap.StarRating > beatmap.StarRating && otherBeatmap.GeneralSettings.mode == beatmap.GeneralSettings.mode);

                Parallel.ForEach(otherBeatmaps, otherBeatmap => PopulateInconsistencies(beatmap, otherBeatmap));

                foreach (var inconsistentEdgeTime in inconsistencies.Select(inconsistency => inconsistency.inconsistentEdgeTime).Distinct())
                {
                    var respectiveBeatmap = inconsistencies.Where(inconsistency => inconsistency.inconsistentEdgeTime.AlmostEqual(inconsistentEdgeTime)).Select(inconsistency => inconsistency.respectiveBeatmap).First();

                    var respectiveEdgeTime = inconsistencies.Where(inconsistency => inconsistency.inconsistentEdgeTime.AlmostEqual(inconsistentEdgeTime)).Select(inconsistency => inconsistency.respectiveEdgeTime).FirstOrDefault();

                    if (beatmap.GetLowestDivisor(respectiveEdgeTime) == 0 || beatmap.GetLowestDivisor(inconsistentEdgeTime) == 0)
                        continue;

                    yield return new Issue(GetTemplate("Snap Consistency"), beatmap, Timestamp.Get(respectiveEdgeTime), beatmap.GetLowestDivisor(respectiveEdgeTime), Timestamp.Get(inconsistentEdgeTime), beatmap.GetLowestDivisor(inconsistentEdgeTime), respectiveBeatmap);
                }

                PrepareRareDivisors(beatmap);
                PopulateRareDivisors(beatmap);

                foreach (var issue in GetDivisorIssues(beatmap, countWarningStamps, "Snap Count")) yield return issue;

                foreach (var issue in GetDivisorIssues(beatmap, percentWarningStamps, "Snap Percent"))
                    yield return issue;

                foreach (var issue in GetDivisorIssues(beatmap, countMinorStamps, "Minor Snap Count"))
                    yield return issue;

                foreach (var issue in GetDivisorIssues(beatmap, percentMinorStamps, "Minor Snap Percent"))
                    yield return issue;
            }
        }

        /// <summary> Finds which divisors are rare in the beatmap so that they can be looked for in other functions. </summary>
        private void PrepareRareDivisors(Beatmap beatmap)
        {
            // rather than calculating every single snapping, this instead just looks for potentially weird snappings
            // this makes it both more convenient, due to constraining unimportant options, and more optimized
            const int countWarning = 3;
            const int countMinor = 7;
            const double percentWarning = 0.005;
            const double percentMinor = 0.05;

            countWarningDivisors = [];
            countMinorDivisors = [];
            percentWarningDivisors = [];
            percentMinorDivisors = [];

            var divisorGroups = beatmap.HitObjects.SelectMany(hitObject => hitObject.GetEdgeTimes().Select(beatmap.GetLowestDivisor)).Where(divisor => divisor != 0).GroupBy(divisor => divisor).Select(group => new
            {
                divisor = group.Key,
                count = group.Count()
            }).ToList();

            var divisorsTotal = divisorGroups.Sum(divisorGroup => divisorGroup.count);

            foreach (var divisorGroup in divisorGroups)
            {
                var precentage = divisorGroup.count / (double)divisorsTotal;

                if (divisorGroup.count <= countWarning) countWarningDivisors.Add(divisorGroup.divisor);
                else if (divisorGroup.count <= countMinor) countMinorDivisors.Add(divisorGroup.divisor);

                if (precentage < percentWarning) percentWarningDivisors.Add(divisorGroup.divisor);
                else if (precentage < percentMinor) percentMinorDivisors.Add(divisorGroup.divisor);
            }
        }

        /// <summary> Populates the warnings and minor issues for rare divisors in the given beatmap. </summary>
        private void PopulateRareDivisors(Beatmap beatmap)
        {
            countWarningStamps = [];
            countMinorStamps = [];
            percentWarningStamps = [];
            percentMinorStamps = [];

            foreach (var hitObject in beatmap.HitObjects)
                foreach (var edgeTime in hitObject.GetEdgeTimes())
                    TryAddDivisorIssue(edgeTime, beatmap);

            countWarningStamps = countWarningStamps.Distinct().ToList();
            countMinorStamps = countMinorStamps.Distinct().ToList();
            percentWarningStamps = percentWarningStamps.Distinct().ToList();
            percentMinorStamps = percentMinorStamps.Distinct().ToList();
        }

        /// <summary>
        ///     Supplied with the divisor issue list, this function basically just turns them into readable issues
        ///     which we can then return in GetIssues.
        /// </summary>
        private IEnumerable<Issue> GetDivisorIssues(Beatmap beatmap, IReadOnlyCollection<Tuple<int, string>> divisorTupleList, string templateKey)
        {
            if (divisorTupleList.Count == 0)
                yield break;

            foreach (var divisor in divisorTupleList.Select(stamp => stamp.Item1).Distinct())
            {
                var stamps = divisorTupleList.Where(stamp => stamp.Item1 == divisor).Select(stamp => stamp.Item2);

                yield return new Issue(GetTemplate(templateKey), beatmap, string.Join(" ", stamps), divisor);
            }
        }

        /// <summary>
        ///     Adds the given time value as a divisor issue if its divisor is really rare in the beatmap, and it isn't
        ///     unsnapped.
        /// </summary>
        private void TryAddDivisorIssue(double time, Beatmap beatmap)
        {
            // no need to double error, unsnap check will take care of this
            if (beatmap.GetUnsnapIssue(time) != null)
                return;

            var divisor = beatmap.GetLowestDivisor(time);

            if (!countWarningDivisors.Contains(divisor) && !percentWarningDivisors.Contains(divisor) && !countMinorDivisors.Contains(divisor) && !percentMinorDivisors.Contains(divisor))
                return;

            if (countWarningDivisors.Contains(divisor))
                countWarningStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (percentWarningDivisors.Contains(divisor))
                percentWarningStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (countMinorDivisors.Contains(divisor))
                countMinorStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
            else if (percentMinorDivisors.Contains(divisor))
                percentMinorStamps.Add(new Tuple<int, string>(divisor, Timestamp.Get(time)));
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

        /// <summary>
        ///     Returns any time value from the first beatmap that has no corresponding
        ///     object within 3 ms in the other beatmap.
        /// </summary>
        private static IEnumerable<double> GetMissingEdgeTimes(Beatmap beatmap, Beatmap otherBeatmap)
        {
            var edgeTimes = new List<double>();

            foreach (var hitObject in beatmap.HitObjects)
                edgeTimes.AddRange(hitObject.GetEdgeTimes());

            var otherEdgeTimes = new List<double>();

            foreach (var otherHitObject in otherBeatmap.HitObjects)
                otherEdgeTimes.AddRange(otherHitObject.GetEdgeTimes());

            foreach (var edgeTime in edgeTimes)
                if (!otherEdgeTimes.Exists(time => Math.Abs(time - edgeTime) < 3))
                    yield return edgeTime;
        }

        private IEnumerable<Inconsistency> GetInconsistencies(Beatmap beatmap, Beatmap otherBeatmap)
        {
            foreach (var missingEdgeTime in GetMissingEdgeTimes(beatmap, otherBeatmap))
                foreach (var otherMissingEdgeTime in GetMissingEdgeTimes(otherBeatmap, beatmap))
                {
                    var timeDifference = Math.Abs(missingEdgeTime - otherMissingEdgeTime);

                    if (timeDifference <= 3)
                        // If both maps somehow claim they have an object the other does not at the same time, we skip that case.
                        continue;

                    var line = otherBeatmap.GetTimingLine<UninheritedLine>(missingEdgeTime);
                    var msPerBeat = line.msPerBeat;

                    if (timeDifference >= msPerBeat)
                        // Edges a beat apart or more should not be flagged as inconsistent, so skip those cases.
                        continue;

                    var consistencyRange = GetConsistencyRange(otherBeatmap, missingEdgeTime, msPerBeat, otherMissingEdgeTime);

                    if (missingEdgeTime + consistencyRange > otherMissingEdgeTime && missingEdgeTime - consistencyRange < otherMissingEdgeTime)
                        yield return new Inconsistency(missingEdgeTime, otherMissingEdgeTime, otherBeatmap);
                }
        }

        /// <summary>
        ///     Gets a time offset from a given divisor which may be confused with it. Larger for smaller divisors.
        ///     So the offset for a 1/1 would be larger than for a 1/6, for example. If two times are supplied, the largest divisor
        ///     is used.
        /// </summary>
        private double GetConsistencyRange(Beatmap otherBeatmap, double time, double msPerBeat, double? otherTime = null)
        {
            var divisor = Math.Max(otherBeatmap.GetLowestDivisor(time), 2);

            if (otherTime == null)
            {
                var divisorIndex = Array.IndexOf(divisors, divisor);

                var greaterDivisor = divisors.ElementAt(divisorIndex + 2 > divisors.Length - 1 ? divisors.Length - 1 : divisorIndex + 2);

                const int unsnapMargin = 2;

                return msPerBeat / greaterDivisor - unsnapMargin;
            }

            var higherDiffDivisor = Math.Max(otherBeatmap.GetLowestDivisor(otherTime.GetValueOrDefault()), 2);

            // If the higher difficulty uses higher snaps, that's assumed to be normal progression,
            // unless we go from 1/3 to 1/4 or similar, which would be pretty odd.
            if (divisor < higherDiffDivisor || (divisor % 3 != 0 && higherDiffDivisor % 3 == 0))
                return Math.Max(GetConsistencyRange(otherBeatmap, time, msPerBeat), GetConsistencyRange(otherBeatmap, otherTime.GetValueOrDefault(), msPerBeat));

            return 2;
        }

        private readonly struct Inconsistency
        {
            public readonly double inconsistentEdgeTime;
            public readonly double respectiveEdgeTime;
            public readonly Beatmap respectiveBeatmap;

            public Inconsistency(double inconsistentEdgeTime, double respectiveEdgeTime, Beatmap respectiveBeatmap)
            {
                this.inconsistentEdgeTime = inconsistentEdgeTime;
                this.respectiveEdgeTime = respectiveEdgeTime;
                this.respectiveBeatmap = respectiveBeatmap;
            }
        }
    }
}