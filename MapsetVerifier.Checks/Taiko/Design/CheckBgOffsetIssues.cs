using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Taiko.Design
{
    [Check]
    public class CheckBgOffsetIssues : BeatmapSetCheck
    {
        private const string Inconsistent = nameof(Inconsistent);
        private const string XOffset = nameof(XOffset);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Nostril, Hivie",
                Category = "Design",
                Modes = [Beatmap.Mode.Taiko],
                Message = "Background offset issues",
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Pointing out background offset issues between osu!taiko difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Background offset should generally be consistent across osu!taiko difficulties that share the same background.
                    
                    In addition, horizontal (X axis) offset is very rare and often leaves black gaps in the playfield, so it should be double-checked when present."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Inconsistent,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" {1}: ({2})",
                        "Filename",
                        "Offset Coordinates",
                        "List of Difficulties"
                    ).WithCause(
                        "Background offset is inconsistent across difficulties. Make sure this is intentional."
                    )
                },
                {
                    XOffset,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" uses an X offset of {1}. Ensure this is intentional.",
                        "Filename",
                        "X offset"
                    ).WithCause(
                        "Horizontal background offset is rare in osu!taiko and often leaves black gaps in the playfield."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Record known offsets for each unique BG file
            var files = new Dictionary<string, Dictionary<Vector2, HashSet<string>>>();

            // Filter to osu!taiko beatmaps only
            var taikoBeatmaps = beatmapSet
                .Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
                .ToList();

            foreach (var beatmap in taikoBeatmaps)
            {
                foreach (var beatmapBg in beatmap.Backgrounds)
                {
                    var path = beatmapBg.path;

                    if (path == null)
                        continue;

                    var offset = beatmapBg.offset;
                    if (offset == null)
                        continue;

                    var castOffset = (Vector2)offset;

                    if (castOffset.X != 0)
                    {
                        yield return new Issue(GetTemplate(XOffset), beatmap, path, castOffset.X);
                    }

                    files.TryAdd(path, new Dictionary<Vector2, HashSet<string>>());
                    var file = files[path];
                    file.TryAdd(castOffset, []);

                    file[castOffset].Add(beatmap.MetadataSettings.version);
                }
            }

            // Print any inconsistencies
            foreach (var file in files)
            {
                var fileName = file.Key;
                var offsets = file.Value;

                // If the file only has a single recorded offset, there is no inconsistency
                if (offsets.Count <= 1)
                    continue;

                foreach (var offset in offsets)
                {
                    var offsetCoords = offset.Key;
                    var diffNames = string.Join(", ", offset.Value);
                    yield return new Issue(
                        GetTemplate(Inconsistent),
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
