using System.Drawing;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Storyboard;

[Check]
public class CheckStoryboardOffScreen : GeneralCheck
{
    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Storyboard",
            Message = "Sprites that are never on screen.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Pointing out sprites which are off-screen for as long as they are active."
                },
                {
                    "Reasoning",
                    @"
                    Nobody gets to see these, yet they are still loaded and take up performance.
                    
                    This includes the space to the sides that is only visible with widescreen support. Sprites with a missing image are
                    not included."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Off-screen",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} never on screen, see {1}.",
                    "sprites",
                    "sprites"
                )
                    .WithCause(
                        "A sprite or animation is outside of the screen from its first to its last command."
                    )
                    .WithRule(RC.General.Storyboarding_AvoidSpritesBeingPartiallyOff)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        // The same images tend to be used by many sprites.
        var sizes = new Dictionary<string, Size?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (beatmap, elements) in StoryboardPatterns.GetStoryboards(beatmapSet))
        {
            var places = elements
                .Where(element =>
                    GetSize(beatmapSet, element, sizes) is { } size
                    && StoryboardPatterns.IsNeverOnScreen(element, size.Width, size.Height)
                )
                .Select(element => (Element: element, Time: element.GetActiveTime()?.StartTime))
                .ToList();

            if (places.Count > 0)
                yield return new Issue(
                    GetTemplate("Off-screen"),
                    beatmap,
                    places.Count == 1 ? "1 sprite is" : $"{places.Count} sprites are",
                    StoryboardPatterns.Describe(places)
                );
        }
    }

    /// <summary> Returns the largest size the sprite has in any frame, or null if any of those can't be read. </summary>
    private static Size? GetSize(
        BeatmapSet beatmapSet,
        Sprite element,
        Dictionary<string, Size?> sizes
    )
    {
        var paths = element is Animation animation ? animation.framePaths : [element.path ?? ""];
        var largest = new Size();

        foreach (var path in paths)
        {
            if (path == "" || path.StartsWith(".."))
                return null;

            if (!sizes.TryGetValue(path, out var size))
                sizes[path] = size = ImageSize.Read(Path.Combine(beatmapSet.SongPath, path));

            if (size == null)
                return null;

            largest = new Size(
                Math.Max(largest.Width, size.Value.Width),
                Math.Max(largest.Height, size.Value.Height)
            );
        }

        return largest.Width > 0 ? largest : null;
    }
}
