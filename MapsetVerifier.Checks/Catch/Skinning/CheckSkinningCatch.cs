using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using SixLabors.ImageSharp;

namespace MapsetVerifier.Checks.Catch.Skinning;

[Check]
public class CheckSkinningCatch : BeatmapSetCheck
{
    private static bool HasDroplets(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Catch
            && beatmap.HitObjects.Any(hitObject => hitObject is Slider)
        );

    private static bool HasBananas(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Catch
            && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)
        );

    private static readonly SkinElement[] FruitElements =
    [
        new SkinElement("fruit-apple.png", true),
        new SkinElement("fruit-apple-overlay.png", true),
        new SkinElement("fruit-grapes.png", true),
        new SkinElement("fruit-grapes-overlay.png", true),
        new SkinElement("fruit-orange.png", true),
        new SkinElement("fruit-orange-overlay.png", true),
        new SkinElement("fruit-pear.png", true),
        new SkinElement("fruit-pear-overlay.png", true),
        new SkinElement("fruit-bananas.png", true, HasBananas),
        new SkinElement("fruit-bananas-overlay.png", true, HasBananas),
    ];

    private static readonly SkinElement[] DropElements =
    [
        new SkinElement("fruit-drop.png", true, HasDroplets),
        new SkinElement("fruit-drop-overlay.png", true, HasDroplets),
    ];

    private static readonly SkinElement[] CatcherElements =
    [
        new SkinElement("fruit-catcher-fail-{n}.png", true),
        new SkinElement("fruit-catcher-idle-{n}.png", true),
        new SkinElement("fruit-catcher-kiai-{n}.png", true),
    ];

    private static readonly SkinSet FruitsSet = new(
        "Fruits",
        FruitElements.Concat(DropElements).ToArray()
    );

    private static readonly SkinSet CatcherSet = new(
        "Catcher",
        CatcherElements.Concat([new SkinElement("lighting.png", false)]).ToArray()
    );

    private static readonly SkinSet[] Sets = [FruitsSet, CatcherSet];

    // Ranking criteria-mandated dimensions; skinned elements must match their default skin
    // counterparts exactly so they represent the hitbox correctly and don't alter gameplay.
    private const int FruitSize = 128;
    private const int DropWidth = 82;
    private const int DropHeight = 103;
    private const int CatcherWidth = 306;
    private const int CatcherHeight = 320;

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Catch],
            Category = "Skinning",
            Message = "Incomplete or incorrect skin element sets.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that beatmap-specific skin elements are complete sets, use appropriate file formats,
                    and follow the osu!catch ranking criteria's skinning rules."
                },
                {
                    "Reasoning",
                    @"
                    When skinning a gameplay element, complete sets of elements need to be skinned in order to
                    avoid conflicts between user-specific and beatmap-specific skins. If a required element of a
                    partially-skinned set is missing, the user-specific skin's version of that element may be
                    used instead, which can look inconsistent or even unreadable together with the rest of the
                    beatmap skin.

                    Fruits, drops, and the catcher must also match their default skin dimensions exactly, since
                    they represent the in-game hitbox and altering their size can change gameplay, and custom
                    catchers require the skin.ini to declare a v2 (or higher) skin format.
                    "
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Incomplete Set",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "The \"{0}\" skin set is only partially skinned, missing: {1}.",
                    "set",
                    "missing elements"
                ).WithCause(
                    "One or more elements of a skin set are present, but a required element of the same set is missing."
                )
            },
            {
                "Incorrect Dimensions",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" is {1}x{2}, but must be exactly {3}x{4}.",
                    "path",
                    "actual width",
                    "actual height",
                    "expected width",
                    "expected height"
                ).WithCause(
                    "A skinned fruit, drop, or catcher element does not match its default skin counterpart's dimensions."
                )
            },
            {
                "Old Skin Version",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "A custom catcher is skinned, but skin.ini does not declare a v2 (or higher) skin format."
                ).WithCause(
                    "Custom catchers must be included in the v2 skin format, declared via \"Version\" under [General] in skin.ini."
                )
            },
            {
                "Non Png",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "\"{0}\" should be a .png file if it uses transparency.",
                    "path"
                ).WithCause(
                    "A skinned gameplay element does not use the .png format, which is required for elements that utilise transparency."
                )
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        foreach (var issue in GetSetIssues(beatmapSet))
            yield return issue;

        foreach (var issue in GetDimensionIssues(FruitElements, beatmapSet, FruitSize, FruitSize))
            yield return issue;

        foreach (var issue in GetDimensionIssues(DropElements, beatmapSet, DropWidth, DropHeight))
            yield return issue;

        foreach (
            var issue in GetDimensionIssues(
                CatcherElements,
                beatmapSet,
                CatcherWidth,
                CatcherHeight
            )
        )
            yield return issue;

        if (
            SkinSetUtils.IsSetSkinned(CatcherSet, beatmapSet)
            && !HasV2OrHigherSkinVersion(beatmapSet)
        )
            yield return new Issue(GetTemplate("Old Skin Version"), null);
    }

    private IEnumerable<Issue> GetSetIssues(BeatmapSet beatmapSet)
    {
        foreach (var set in Sets)
        {
            if (!SkinSetUtils.IsSetSkinnedAndRelevant(set, beatmapSet))
                continue;

            var missing = SkinSetUtils.GetMissingRequiredElements(set, beatmapSet).ToList();

            if (missing.Count > 0)
                yield return new Issue(
                    GetTemplate("Incomplete Set"),
                    null,
                    set.Name,
                    string.Join(", ", missing.Select(element => element.Pattern))
                );

            foreach (var element in set.Elements)
            {
                var path = SkinSetUtils.GetPresentPath(element, beatmapSet);

                if (path != null && !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    yield return new Issue(GetTemplate("Non Png"), null, PathStatic.CutPath(path));
            }
        }
    }

    private IEnumerable<Issue> GetDimensionIssues(
        SkinElement[] elements,
        BeatmapSet beatmapSet,
        int expectedWidth,
        int expectedHeight
    )
    {
        foreach (var element in elements)
        foreach (var path in SkinSetUtils.GetAllPresentPaths(element, beatmapSet))
        {
            var size = TryGetImageSize(path);

            if (
                size == null
                || (size.Value.Width == expectedWidth && size.Value.Height == expectedHeight)
            )
                continue;

            yield return new Issue(
                GetTemplate("Incorrect Dimensions"),
                null,
                PathStatic.CutPath(path),
                size.Value.Width,
                size.Value.Height,
                expectedWidth,
                expectedHeight
            );
        }
    }

    private static (int Width, int Height)? TryGetImageSize(string path)
    {
        try
        {
            var info = Image.Identify(path);

            return info == null ? null : (info.Width, info.Height);
        }
        catch (Exception ex) when (ex is IOException or UnknownImageFormatException)
        {
            return null;
        }
    }

    private static bool HasV2OrHigherSkinVersion(BeatmapSet beatmapSet)
    {
        var skinIniPath = beatmapSet.SongFilePaths.FirstOrDefault(path =>
            string.Equals(PathStatic.CutPath(path), "skin.ini", StringComparison.OrdinalIgnoreCase)
        );

        if (skinIniPath == null)
            return false;

        string[] lines;

        try
        {
            lines = File.ReadAllLines(skinIniPath);
        }
        catch (IOException)
        {
            return false;
        }

        var versionLine = lines.FirstOrDefault(line =>
            line.TrimStart().StartsWith("Version", StringComparison.OrdinalIgnoreCase)
        );

        if (versionLine == null)
            return false;

        var value = versionLine.Split(':', 2).ElementAtOrDefault(1)?.Trim();

        if (value == null)
            return false;

        if (string.Equals(value, "latest", StringComparison.OrdinalIgnoreCase))
            return true;

        return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var version
            )
            && version >= 2;
    }
}
