using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Mania.HitSounds;

[Check]
public class CheckManiaStoryHitSounds : BeatmapSetCheck
{
    /// <summary> A sample at most this many volume points quieter than the notes is as audible as they are. </summary>
    private const float SimilarVolume = 10;

    /// <summary> How many timestamps the issue shows, the rest are only counted. </summary>
    private const int ShownTimestamps = 5;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Hit Sounds",
            Message = "Storyboarded hit sounds where a note could play them.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Finding storyboarded hit sounds that are about as loud as the notes or louder, at a time where a
                    note without any hit sound of its own could play them instead."
                },
                {
                    "Reasoning",
                    @"
                    Storyboarded hit sounds play no matter what the player does. When they are as loud as the notes, they
                    can drown out the feedback of the notes, so hit sounds belong on a note where possible."
                },
                {
                    "Exceptions",
                    @"
                    Samples are fine when there is no note left to play them, as is common in low difficulties. One
                    note of each chord is kept for the hit normal, unless a note with another hit sound already plays it.

                    Keysounded difficulties, where at least half of the notes play a file of their own, are left out."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Could Be On Note",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} {1} storyboarded hit sound{2} as loud as the notes could be on a note instead, such as \"{3}\".",
                    "timestamps -",
                    "amount",
                    "s",
                    "sample"
                )
                    .WithCause(
                        "A storyboarded hit sound is about as loud as the notes at the same time or louder, and one of those notes has no hit sound of its own."
                    )
                    .WithRule(RC.Mania.AvoidUsingStoryboardSampledHitsounds)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (
            var beatmap in beatmapSet.Beatmaps.Where(beatmap =>
                beatmap.GeneralSettings.mode == Beatmap.Mode.Mania
            )
        )
        {
            var found = FindMovableSamples(beatmap, beatmapSet.Osb?.samples ?? []);

            if (found.Count == 0)
                continue;

            var timestamps = found
                .Select(sample => ManiaHitSounds.TimeKey(sample.time))
                .Distinct()
                .Take(ShownTimestamps)
                .Select(time => Timestamp.Get(time));

            yield return new Issue(
                GetTemplate("Could Be On Note"),
                beatmap,
                string.Concat(timestamps),
                found.Count,
                found.Count == 1 ? "" : "s",
                found[0].path
            );
        }
    }

    /// <summary> Returns the storyboarded samples as loud as the notes that one of those notes could play. </summary>
    private static List<Sample> FindMovableSamples(Beatmap beatmap, List<Sample> osbSamples)
    {
        var found = new List<Sample>();
        var notes = beatmap.HitObjects;

        // Samples on the fail layer are only heard while failing.
        var samples = beatmap
            .Samples.Concat(osbSamples)
            .Where(sample => sample.layer != Sample.Layer.Fail && sample.volume > 0)
            .ToLookup(sample => ManiaHitSounds.TimeKey(sample.time));

        if (samples.Count == 0 || notes.Count(note => note.filename != null) * 2 >= notes.Count)
            return found;

        foreach (
            var chord in notes
                .GroupBy(note => ManiaHitSounds.TimeKey(note.time))
                .OrderBy(chord => chord.Key)
        )
        {
            if (!samples.Contains(chord.Key))
                continue;

            var free = chord.Count(IsFree);

            // A note playing a file has no hit normal, so without additions a free note has to keep it.
            if (chord.All(note => IsFree(note) || note.filename != null))
                free--;

            if (free <= 0)
                continue;

            var loudest = chord.Max(GetVolume);

            found.AddRange(
                samples[chord.Key]
                    .Where(sample => sample.volume >= loudest - SimilarVolume)
                    .Take(free)
            );
        }

        return found;
    }

    /// <summary> Whether the note plays nothing but its hit normal. </summary>
    private static bool IsFree(HitObject note) =>
        ManiaHitSounds.GetSamples(note).All(sample => sample.Kind == ManiaSampleKind.HitNormal);

    private static float GetVolume(HitObject note) =>
        note.volume ?? note.beatmap.GetTimingLine(note.time, true)?.Volume ?? 100;
}
