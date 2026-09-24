using System.Globalization;
using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Taiko.Design
{
    [Check]
    public class CheckBgOffsetIssues : BeatmapSetCheck
    {
        private const string Inconsistent = nameof(Inconsistent);
        private const string XOffset = nameof(XOffset);
        private const string YOffset = nameof(YOffset);

        // Scale every background to this width. The offset may consume whatever height is left
        // after 140 pixels, which is the pair that yields 115 at 16:9 and 200 at 4:3.
        private const double ScaledWidth = 1360d / 3d;
        private const double CoveredHeight = 140;

        internal static int MaxVerticalOffset(double width, double height) =>
            (int)Math.Floor(ScaledWidth * height / width - CoveredHeight);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Nostril",
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
                    
                    In addition, horizontal (X axis) offset is very rare and often leaves black gaps in the playfield, so it should be double-checked when present.

                    Vertical offset also can't be too high, as it will cause a gap under the playfield. The limit changes depending on the aspect ratio, for example:
                    - 16:9: 115
                    - 4:3: 200
                    
                    Limit is intentionally slightly lower than the maximum possible value, to account for potential miscalculations."
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
                {
                    YOffset,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\"'s vertical offset ({1}) may cause a gap under the playfield. Consider using a value below {2}.",
                        "Filename",
                        "Vertical offset",
                        "Limit"
                    ).WithCause(
                        "Vertical offset is high enough that the background no longer covers the playfield for its aspect ratio."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Record known offsets for each unique BG file
            var files = new Dictionary<string, Dictionary<Vector2, HashSet<string>>>();
            var imageSizes = new Dictionary<string, (int Width, int Height)?>(
                StringComparer.OrdinalIgnoreCase
            );

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

                    if (!imageSizes.TryGetValue(path, out var imageSize))
                    {
                        imageSize = ReadImageSize(beatmap, path);
                        imageSizes[path] = imageSize;
                    }

                    if (imageSize is { Width: > 0, Height: > 0 } size && castOffset.Y > 0)
                    {
                        var limit = MaxVerticalOffset(size.Width, size.Height);
                        if (castOffset.Y >= limit)
                        {
                            yield return new Issue(
                                GetTemplate(YOffset),
                                beatmap,
                                path,
                                castOffset.Y,
                                limit.ToString(CultureInfo.InvariantCulture)
                            );
                        }
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
                    var offsetCoordsText = string.Create(
                        CultureInfo.InvariantCulture,
                        $"({offsetCoords.X}, {offsetCoords.Y})"
                    );
                    var diffNames = string.Join(", ", offset.Value);
                    yield return new Issue(
                        GetTemplate(Inconsistent),
                        null,
                        fileName,
                        offsetCoordsText,
                        diffNames
                    );
                }
            }
        }

        private static (int Width, int Height)? ReadImageSize(Beatmap beatmap, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(beatmap.SongPath))
                return null;

            var fullPath = Path.Combine(
                beatmap.SongPath,
                relativePath
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
            );
            if (!File.Exists(fullPath))
                return null;

            var tagFile = new FileAbstraction(fullPath).GetTagFile();
            if (tagFile == null)
                return null;

            var width = tagFile.Properties.PhotoWidth;
            var height = tagFile.Properties.PhotoHeight;
            if (width <= 0 || height <= 0)
                return null;

            return (width, height);
        }
    }
}
