using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    [Check]
    public class CheckDoubleHitSounds : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Hit Sounds",
                Message = "The same sample played multiple times at once.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing a chord from stacking the same sample on top of itself."
                    },
                    {
                        "Reasoning",
                        @"
                        Notes hit at the same time all play their samples at the same time, so putting the same
                        addition on two notes of a chord plays that sample twice and roughly doubles its volume.
                        Mania hit sounding convention is to place an addition on a single note of the chord, so
                        this is usually an accident from copying a note or from selecting a whole column."
                    },
                    {
                        "Exceptions",
                        @"
                        The hit normal is ignored, since every note plays one and stacking those is unavoidable
                        rather than a mistake."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Double",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1} is played {2} times at once, ensure this is intentional.",
                        "timestamp -",
                        "sample",
                        "amount"
                    ).WithCause(
                        "Multiple notes hit at the same time play the same sample, doubling its volume."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (
                var chord in beatmap.HitObjects.GroupBy(hitObject =>
                    ManiaHitSounds.TimeKey(hitObject.time)
                )
            )
            {
                var notes = chord.ToList();

                if (notes.Count < 2)
                    continue;

                var samplesPlayed = new Dictionary<string, (string Display, int Count)>();

                foreach (var hitObject in notes)
                foreach (
                    var sample in ManiaHitSounds
                        .GetSamples(hitObject)
                        .Where(sample => sample.Kind != ManiaSampleKind.HitNormal)
                        .DistinctBy(sample => sample.Name)
                )
                {
                    var played = samplesPlayed.TryGetValue(sample.Name, out var existing)
                        ? existing
                        : (Display: sample.Display, Count: 0);

                    samplesPlayed[sample.Name] = (played.Display, played.Count + 1);
                }

                foreach (
                    var (display, count) in samplesPlayed.Values.Where(played => played.Count > 1)
                )
                    yield return new Issue(
                        GetTemplate("Double"),
                        beatmap,
                        Timestamp.Get(chord.Key),
                        display,
                        count
                    );
            }
        }
    }
}
