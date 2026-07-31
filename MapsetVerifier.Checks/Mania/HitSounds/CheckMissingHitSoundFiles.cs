using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    [Check]
    public class CheckMissingHitSoundFiles : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Hit Sounds",
                Message = "Hit sounds referenced without the file being present.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring every sample a difficulty asks for is actually included in the mapset."
                    },
                    {
                        "Reasoning",
                        @"
                        Mania hit sounding is built almost entirely out of custom samples, so a sample that was
                        renamed, moved out of the folder or never included in the first place silently plays
                        something else than intended. Since nothing about it is audible as an error, it's easy
                        to miss until someone plays the map with a different skin."
                    },
                    {
                        "Exceptions",
                        @"
                        Samples on custom index 0 or 1 are left alone, as those intentionally fall back to the
                        player's skin or the game's own samples rather than coming from the mapset."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Missing File",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} is used at {1}, but no such file exists in the mapset.",
                        "file name",
                        "timestamp -"
                    ).WithCause(
                        "A hit object references a hit sound file which isn't present in the song folder."
                    )
                },
                {
                    "Missing Sample",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} is used at {1}, but no such file exists in the mapset, so the default is played instead.",
                        "file name",
                        "timestamp -"
                    ).WithCause(
                        "A custom sample index is used at this point in time, but the mapset contains no sample "
                            + "for that combination of sampleset, addition and index."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var maniaBeatmaps = beatmapSet
                .Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                .ToList();

            if (maniaBeatmaps.Count == 0)
                yield break;

            var presentFiles = ManiaHitSounds.GetSampleFileNames(beatmapSet);

            foreach (var beatmap in maniaBeatmaps)
            {
                // The same missing sample is usually referenced hundreds of times over, so we only point out
                // the first use of each rather than burying the mapper in identical issues.
                var reported = new HashSet<string>();

                foreach (var hitObject in beatmap.HitObjects)
                foreach (var sample in ManiaHitSounds.GetSamples(hitObject))
                {
                    if (
                        !sample.RequiresFileInMapset()
                        || presentFiles.Contains(sample.ExpectedFileName())
                    )
                        continue;

                    if (!reported.Add(sample.Name))
                        continue;

                    var template =
                        sample.Kind == ManiaSampleKind.CustomFile
                            ? "Missing File"
                            : "Missing Sample";

                    yield return new Issue(
                        GetTemplate(template),
                        beatmap,
                        sample.Display,
                        Timestamp.Get(hitObject.time)
                    );
                }
            }
        }
    }
}
