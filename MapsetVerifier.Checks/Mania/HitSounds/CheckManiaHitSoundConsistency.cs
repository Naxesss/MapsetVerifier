using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    [Check]
    public class CheckManiaHitSoundConsistency : BeatmapSetCheck
    {
        /// <summary>
        ///     Minimum number of samples a difficulty needs before it's considered for exclusion. Below this
        ///     there's too little to tell "uses its own hit sounding" apart from "missed a couple of additions".
        /// </summary>
        private const int MinSamplesForExclusion = 8;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Hit Sounds",
                Message = "Inconsistent hit sounds between difficulties.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring hit sounds are copied down consistently between difficulties of the same
                        beatmapset."
                    },
                    {
                        "Reasoning",
                        @"
                        Mania difficulties are usually hit sounded once and then copied over to the rest of the
                        set. Notes that exist in both difficulties are expected to sound the same, so a sample
                        which one difficulty plays and another doesn't is usually an addition that was missed
                        while copying."
                    },
                    {
                        "Exceptions",
                        @"
                        Only points in time where both difficulties actually have a note are compared, since a
                        difficulty can't play a sample where it has nothing to hit.

                        The hit normal is left out of the comparison. It's driven by the timing lines rather
                        than by individual notes, and inconsistencies there are already pointed out by the
                        inconsistent timing lines check.

                        Difficulties which appear to use their own, unrelated hit sounding (for example a guest
                        difficulty using samples from a different set) are excluded from the comparison, since
                        they're expected to be inconsistent with the rest."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Missing",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} is missing ({1}) which exists in {2}.",
                        "timestamp -",
                        "sample",
                        "other difficulties"
                    ).WithCause(
                        "A sample is played at this time in another difficulty which also has a note here, but not in this one."
                    )
                },
                {
                    "Missing Minor",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} is missing ({1}) which exists in {2}.",
                        "timestamp -",
                        "sample",
                        "other difficulties"
                    ).WithCause(
                        "Same as the warning, but only a minority of the other difficulties play this sample, "
                            + "so it's more likely to be intentional."
                    )
                },
                {
                    "Unique Hit Sounds",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "This difficulty appears to have its own hit sounding, so it was excluded from the "
                            + "consistency comparison. Make sure this makes sense."
                    ).WithCause(
                        "This difficulty's hit sounds differ from the rest of the beatmapset far more than usual."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var beatmaps = beatmapSet
                .Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                .ToList();

            if (beatmaps.Count < 2)
                yield break;

            var displays = new Dictionary<string, string>();
            var maps = beatmaps.ToDictionary(
                beatmap => beatmap,
                beatmap => Build(beatmap, displays)
            );

            var (comparable, unique) = SplitByConsistency(beatmaps, maps);

            foreach (var beatmap in unique)
                yield return new Issue(GetTemplate("Unique Hit Sounds"), beatmap);

            foreach (var beatmap in comparable)
            foreach (var issue in GetMissingSampleIssues(beatmap, comparable, maps, displays))
                yield return issue;
        }

        /// <summary> Collects, per point in time, which notes exist and which samples they play. </summary>
        private static HitSoundMap Build(Beatmap beatmap, Dictionary<string, string> displays)
        {
            var map = new HitSoundMap();

            foreach (var hitObject in beatmap.HitObjects)
            {
                var time = ManiaHitSounds.TimeKey(hitObject.time);
                map.NoteTimes.Add(time);

                foreach (var sample in ManiaHitSounds.GetSamples(hitObject))
                {
                    // The hit normal follows the timing lines rather than the note, so comparing it here would
                    // just repeat what the inconsistent timing lines check already reports.
                    if (sample.Kind == ManiaSampleKind.HitNormal)
                        continue;

                    map.Add(time, sample.Name);
                    displays[sample.Name] = sample.Display;
                }
            }

            return map;
        }

        /// <summary>
        ///     Roughly measures how inconsistent each beatmap's hit sounds are compared to the rest of the set,
        ///     and excludes any beatmap that stands out far more than the average from the comparison, since
        ///     that likely means it uses its own, unrelated hit sounding rather than having missed additions.
        /// </summary>
        private static (List<Beatmap> Comparable, List<Beatmap> Unique) SplitByConsistency(
            List<Beatmap> beatmaps,
            Dictionary<Beatmap, HitSoundMap> maps
        )
        {
            var inconsistencies = beatmaps.ToDictionary(
                beatmap => beatmap,
                beatmap =>
                    CountInconsistencies(
                        maps[beatmap],
                        beatmaps.Where(other => other != beatmap).Select(other => maps[other])
                    )
            );

            var minInconsistency = inconsistencies.Values.Min();
            var avgInconsistency = inconsistencies.Values.Average();

            var comparable = new List<Beatmap>();
            var unique = new List<Beatmap>();

            foreach (var beatmap in beatmaps)
            {
                var relativeInconsistency = Math.Max(
                    inconsistencies[beatmap] - minInconsistency,
                    0
                );

                if (
                    maps[beatmap].SampleCount >= MinSamplesForExclusion
                    && relativeInconsistency > avgInconsistency
                    && relativeInconsistency > maps[beatmap].SampleCount / 4d
                )
                    unique.Add(beatmap);
                else
                    comparable.Add(beatmap);
            }

            return (comparable, unique);
        }

        /// <summary> Counts, averaged over the other difficulties, how many samples differ at shared notes. </summary>
        private static int CountInconsistencies(HitSoundMap map, IEnumerable<HitSoundMap> others)
        {
            var count = 0;
            var otherCount = 0;

            foreach (var other in others)
            {
                ++otherCount;

                foreach (var time in map.NoteTimes)
                {
                    if (!other.NoteTimes.Contains(time))
                        continue;

                    var mine = map.SamplesAt(time);
                    var theirs = other.SamplesAt(time);

                    count +=
                        mine.Count(sample => !theirs.Contains(sample))
                        + theirs.Count(sample => !mine.Contains(sample));
                }
            }

            return otherCount > 0 ? count / otherCount : count;
        }

        private IEnumerable<Issue> GetMissingSampleIssues(
            Beatmap beatmap,
            List<Beatmap> comparable,
            Dictionary<Beatmap, HitSoundMap> maps,
            Dictionary<string, string> displays
        )
        {
            var map = maps[beatmap];
            var otherBeatmaps = comparable.Where(other => other != beatmap).ToList();

            foreach (var time in map.NoteTimes.OrderBy(time => time))
            {
                var mine = map.SamplesAt(time);

                var missing = new List<string>();
                var diffsWithSample = new List<string>();

                // Only difficulties which have a note here can be expected to play anything, so a difficulty
                // without one neither contributes samples nor counts towards the majority below.
                var othersWithNote = 0;

                foreach (var other in otherBeatmaps)
                {
                    var otherMap = maps[other];

                    if (!otherMap.NoteTimes.Contains(time))
                        continue;

                    ++othersWithNote;

                    var missingHere = otherMap
                        .SamplesAt(time)
                        .Where(sample => !mine.Contains(sample))
                        .ToList();

                    if (missingHere.Count == 0)
                        continue;

                    missing.AddRange(missingHere);
                    diffsWithSample.Add(other.MetadataSettings.version);
                }

                if (missing.Count == 0)
                    continue;

                var template =
                    diffsWithSample.Count * 2 > othersWithNote ? "Missing" : "Missing Minor";

                yield return new Issue(
                    GetTemplate(template),
                    beatmap,
                    Timestamp.Get(time),
                    string.Join(", ", missing.Distinct().Select(name => displays[name])),
                    string.Join(", ", diffsWithSample.Distinct())
                );
            }
        }

        /// <summary> Which notes a difficulty has and which samples they play, indexed by time. </summary>
        private sealed class HitSoundMap
        {
            private static readonly HashSet<string> None = [];

            private readonly Dictionary<long, HashSet<string>> samplesByTime = new();

            public HashSet<long> NoteTimes { get; } = [];

            public int SampleCount { get; private set; }

            public void Add(long time, string sample)
            {
                if (!samplesByTime.TryGetValue(time, out var samples))
                    samplesByTime[time] = samples = [];

                if (samples.Add(sample))
                    ++SampleCount;
            }

            public IReadOnlySet<string> SamplesAt(long time) =>
                samplesByTime.TryGetValue(time, out var samples) ? samples : None;
        }
    }
}
