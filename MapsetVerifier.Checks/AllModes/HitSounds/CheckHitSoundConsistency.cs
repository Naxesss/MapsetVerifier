using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.HitSounds
{
    [Check]
    public class CheckHitSoundConsistency : BeatmapSetCheck
    {
        private record HitSoundEvent(double Time, HitObject.HitSounds HitSound);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Standard, Beatmap.Mode.Catch],
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
                        Hit sounds are usually mapped out on one difficulty and then copied over to the rest.
                        It's easy to miss an addition (whistle, finish or clap) on one or more difficulties
                        while doing so, which this check aims to point out. Sliderbody additions are also
                        flagged since they're rarely intentional and easy to miss."
                    },
                    {
                        "Exceptions",
                        @"
                        Difficulties which appear to use their own, unrelated hit sounding (for example a
                        guest difficulty using hit sounds from a different set) are excluded from the
                        comparison, since they're expected to be inconsistent with the rest."
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
                        "hit sound",
                        "other difficulties"
                    ).WithCause(
                        "A hit sound addition exists at this time in another difficulty, but not this one."
                    )
                },
                {
                    "Missing Minor",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} is missing ({1}) which exists in {2}.",
                        "timestamp -",
                        "hit sound",
                        "other difficulties"
                    ).WithCause(
                        "Same as the warning, but only a minority of the other difficulties have this addition, "
                            + "so it's more likely to be intentional."
                    )
                },
                {
                    "Slider Body",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} This sliderbody has additions, ensure this is intentional.",
                        "timestamp -"
                    ).WithCause("A slider has a whistle, finish or clap addition on its body.")
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
                .Beatmaps.Where(beatmap =>
                    beatmap.GeneralSettings.mode is Beatmap.Mode.Standard or Beatmap.Mode.Catch
                )
                .ToList();

            if (beatmaps.Count < 2)
                yield break;

            var events = beatmaps.ToDictionary(beatmap => beatmap, GetHitSoundEvents);

            var (comparable, unique) = SplitByConsistency(beatmaps, events);

            foreach (var beatmap in unique)
                yield return new Issue(GetTemplate("Unique Hit Sounds"), beatmap);

            foreach (var beatmap in comparable)
            foreach (var issue in GetMissingHitSoundIssues(beatmap, comparable, events))
                yield return issue;

            foreach (var beatmap in beatmaps)
            foreach (var issue in GetSliderBodyIssues(beatmap))
                yield return issue;
        }

        /// <summary> Builds the list of hit sound events (start and, if applicable, end) for a beatmap. </summary>
        private static List<HitSoundEvent> GetHitSoundEvents(Beatmap beatmap) =>
            beatmap
                .HitObjects.SelectMany(hitObject =>
                    new[]
                    {
                        hitObject.GetStartHitSound() is { } start
                            ? new HitSoundEvent(hitObject.time, start)
                            : null,
                        hitObject.GetEndHitSound() is { } end
                            ? new HitSoundEvent(hitObject.GetEndTime(), end)
                            : null,
                    }
                )
                .OfType<HitSoundEvent>()
                .ToList();

        /// <summary> Returns the additions (whistle/finish/clap) present in "to" but missing from "from". </summary>
        private static IEnumerable<HitObject.HitSounds> GetMissingAdditions(
            HitObject.HitSounds from,
            HitObject.HitSounds to
        ) =>
            new[]
            {
                HitObject.HitSounds.Whistle,
                HitObject.HitSounds.Finish,
                HitObject.HitSounds.Clap,
            }.Where(addition => !from.HasFlag(addition) && to.HasFlag(addition));

        private static HitSoundEvent? FindEventAtTime(
            IEnumerable<HitSoundEvent> events,
            double time
        ) => events.FirstOrDefault(other => Math.Abs(other.Time - time) < 2);

        /// <summary>
        ///     Roughly measures how inconsistent each beatmap's hit sounds are compared to the rest of the set,
        ///     and excludes any beatmap that stands out far more than the average from the comparison, since
        ///     that likely means it uses its own, unrelated hit sounding rather than having missed additions.
        /// </summary>
        /// <summary>
        ///     Minimum number of hit sound events a difficulty needs before it's even considered for exclusion.
        ///     Below this, there's too little data to reliably tell "uses its own hit sounding" apart from
        ///     "just missing a couple of additions", so we never exclude it.
        /// </summary>
        private const int MinEventsForExclusion = 8;

        private static (List<Beatmap> Comparable, List<Beatmap> Unique) SplitByConsistency(
            List<Beatmap> beatmaps,
            Dictionary<Beatmap, List<HitSoundEvent>> events
        )
        {
            var inconsistencies = beatmaps.ToDictionary(
                beatmap => beatmap,
                beatmap => CountInconsistencies(beatmap, beatmaps, events)
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
                    events[beatmap].Count >= MinEventsForExclusion
                    && relativeInconsistency > avgInconsistency
                    && relativeInconsistency > events[beatmap].Count / 4d
                )
                    unique.Add(beatmap);
                else
                    comparable.Add(beatmap);
            }

            return (comparable, unique);
        }

        private static int CountInconsistencies(
            Beatmap beatmap,
            List<Beatmap> otherBeatmaps,
            Dictionary<Beatmap, List<HitSoundEvent>> events
        )
        {
            var count = 0;

            foreach (var ev in events[beatmap])
            foreach (var other in otherBeatmaps.Where(other => other != beatmap))
            {
                var otherEvent = FindEventAtTime(events[other], ev.Time);

                if (otherEvent != null && otherEvent.HitSound != ev.HitSound)
                    ++count;
            }

            return otherBeatmaps.Count > 1 ? count / (otherBeatmaps.Count - 1) : count;
        }

        private IEnumerable<Issue> GetMissingHitSoundIssues(
            Beatmap beatmap,
            List<Beatmap> comparable,
            Dictionary<Beatmap, List<HitSoundEvent>> events
        )
        {
            var otherBeatmaps = comparable.Where(other => other != beatmap).ToList();

            foreach (var ev in events[beatmap])
            {
                var missing = new List<HitObject.HitSounds>();
                var diffsWithAddition = new List<string>();

                foreach (var other in otherBeatmaps)
                {
                    var otherEvent = FindEventAtTime(events[other], ev.Time);

                    if (otherEvent == null)
                        continue;

                    var missingHere = GetMissingAdditions(ev.HitSound, otherEvent.HitSound)
                        .ToList();

                    if (missingHere.Count == 0)
                        continue;

                    missing.AddRange(missingHere);
                    diffsWithAddition.Add(other.MetadataSettings.version);
                }

                if (missing.Count == 0)
                    continue;

                var template =
                    diffsWithAddition.Count * 2 > otherBeatmaps.Count ? "Missing" : "Missing Minor";

                yield return new Issue(
                    GetTemplate(template),
                    beatmap,
                    Timestamp.Get(ev.Time),
                    string.Join(", ", missing.Distinct()),
                    string.Join(", ", diffsWithAddition.Distinct())
                );
            }
        }

        private IEnumerable<Issue> GetSliderBodyIssues(Beatmap beatmap) =>
            beatmap
                .HitObjects.OfType<Slider>()
                .Where(slider => slider.hitSound > HitObject.HitSounds.Normal)
                .Select(slider => new Issue(
                    GetTemplate("Slider Body"),
                    beatmap,
                    Timestamp.Get(slider.time)
                ));
    }
}
