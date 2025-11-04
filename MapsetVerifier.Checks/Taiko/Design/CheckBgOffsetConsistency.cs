using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Taiko.Design
{
    [Check]
    public class CheckBgOffsetConsistency : GeneralCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);
        
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril",
                Category = "Design",
                Message = "Background offset inconsistencies",
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Pointing out background offset inconsistencies between osu!taikodifficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Background offset should generally be consistent across osu!taiko difficulties that share the same background."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Minor,
                    new IssueTemplate(Issue.Level.Minor,
                        "\"{0}\" {1}: ({2})",
                        "Filename",
                        "Offset Coordinates",
                        "List of Difficulties")
                    .WithCause("Background offset is inconsistent across difficulties. Make sure this is intentional.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Record known offsets for each unique BG file
            var files = new Dictionary<string, Dictionary<Vector2?, HashSet<string>>>();
            
            // Filter to osu!taiko beatmaps only
            var taikoBeatmaps = beatmapSet.Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko).ToList();
            
            foreach (var beatmap in taikoBeatmaps)
            {
                foreach (var beatmapBg in beatmap.Backgrounds) {
                    files.TryAdd(beatmapBg.path, new Dictionary<Vector2?, HashSet<string>>());
                    var file = files[beatmapBg.path];
                    file.TryAdd(beatmapBg.offset, new HashSet<string>());
                    var diffNames = file[beatmapBg.offset];
                    diffNames.Add(beatmap.MetadataSettings.version);
                }
            }

            // Print any inconsistencies
            foreach (var file in files)
            {
                var fileName = file.Key;
                var offsets = file.Value;

                // If the file only has a single recorded offset, there is no inconsistency
                if (offsets.Count <= 1)
                {
                    continue;
                }

                foreach (var offset in offsets)
                {
                    var offsetCoords = offset.Key;
                    var diffNames = string.Join(", ", offset.Value);
                    yield return new Issue(
                        GetTemplate(Minor),
                        null,
                        fileName,
                        offsetCoords,
                        diffNames
                    );
                }
            }
        }
    }
}
