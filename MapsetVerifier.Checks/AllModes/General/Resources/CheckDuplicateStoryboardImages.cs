using System.Security.Cryptography;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.General.Resources;

[Check]
public class CheckDuplicateStoryboardImages : GeneralCheck
{
    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Resources",
            Message = "Duplicate storyboard images.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out storyboard images that are exact copies of each other."
                },
                {
                    "Reasoning",
                    @"
                    Every copy of an image adds to the download size and is loaded separately in-game, while showing the same
                    thing. Use one file and point every sprite that needs it to that file instead."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Duplicate",
                new IssueTemplate(Issue.Level.Warning, "{0} are the same image.", "file names")
                    .WithCause("Two or more storyboard images have the exact same content.")
                    .WithRule(RC.General.Storyboarding_AvoidDuplicateImageFiles)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        var storyboards = beatmapSet
            .Beatmaps.Select(beatmap => (Sprites: beatmap.Sprites, Animations: beatmap.Animations))
            .Append(
                (
                    Sprites: beatmapSet.Osb?.sprites ?? [],
                    Animations: beatmapSet.Osb?.animations ?? []
                )
            );

        var imagePaths = storyboards
            .SelectMany(storyboard =>
                storyboard
                    .Sprites.Select(sprite => sprite.path)
                    .Concat(storyboard.Animations.SelectMany(animation => animation.framePaths))
            )
            .OfType<string>()
            .Where(path => !path.StartsWith(".."))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var byContent = new Dictionary<string, List<string>>();

        foreach (var path in imagePaths)
        {
            var hash = GetHash(Path.Combine(beatmapSet.SongPath, path));

            if (hash == null)
                continue;

            if (!byContent.TryGetValue(hash, out var paths))
                byContent[hash] = paths = [];

            paths.Add(path);
        }

        foreach (var paths in byContent.Values.Where(paths => paths.Count > 1))
            yield return new Issue(
                GetTemplate("Duplicate"),
                null,
                string.Join(
                    ", ",
                    paths.Order(StringComparer.OrdinalIgnoreCase).Select(path => $"\"{path}\"")
                )
            );
    }

    /// <summary> Missing or unreadable images are reported by other checks, so they are skipped here. </summary>
    private static string? GetHash(string fullPath)
    {
        if (!File.Exists(fullPath))
            return null;

        try
        {
            using var stream = File.OpenRead(fullPath);

            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "Couldn't read storyboard image {Path}", fullPath);

            return null;
        }
    }
}
