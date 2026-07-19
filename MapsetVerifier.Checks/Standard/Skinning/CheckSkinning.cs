using System.Numerics;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Skinning;

[Check]
public class CheckSkinning : BeatmapSetCheck
{
    // A heuristic Euclidean distance (out of a max of ~441) below which two RGB colours are
    // considered too visually similar to tell apart as separate slider elements.
    private const float SliderColourSimilarityThreshold = 30f;

    private static bool HasSliders(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Standard
            && beatmap.HitObjects.Any(hitObject => hitObject is Slider)
        );

    private static bool HasRepeatSliders(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Standard
            && beatmap.HitObjects.Any(hitObject => (hitObject as Slider)?.EdgeAmount > 1)
        );

    private static bool HasSpinners(BeatmapSet beatmapSet) =>
        beatmapSet.Beatmaps.Any(beatmap =>
            beatmap.GeneralSettings.mode == Beatmap.Mode.Standard
            && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)
        );

    private static readonly SkinSet CursorSet = new(
        "Cursor",
        new SkinElement("cursor.png", true),
        new SkinElement("cursortrail.png", true),
        new SkinElement("cursormiddle.png", false),
        new SkinElement("cursor-smoke.png", false)
    );

    private static readonly SkinSet HitburstSet = new(
        "Hitburst",
        new SkinElement("hit0-{n}.png", true),
        new SkinElement("hit50-{n}.png", true),
        new SkinElement("hit100-{n}.png", true),
        new SkinElement("hit100k-{n}.png", true),
        new SkinElement("hit300-{n}.png", true),
        new SkinElement("hit300g-{n}.png", true),
        new SkinElement("hit300k-{n}.png", true),
        new SkinElement("particle50.png", false),
        new SkinElement("particle100.png", false),
        new SkinElement("particle300.png", false),
        new SkinElement("sliderpoint10.png", false),
        new SkinElement("sliderpoint30.png", false)
    );

    private static readonly SkinSet HitcircleSet = new(
        "Hitcircle",
        new SkinElement("approachcircle.png", true),
        new SkinElement("followpoint-{n}.png", true),
        new SkinElement("hitcircle.png", true),
        new SkinElement("hitcircleoverlay-{n}.png", true),
        new SkinElement("reversearrow.png", true, HasRepeatSliders),
        new SkinElement("sliderendcircle.png", true, HasSliders),
        new SkinElement("sliderendcircleoverlay-{n}.png", true, HasSliders),
        new SkinElement("sliderstartcircle.png", true, HasSliders),
        new SkinElement("sliderstartcircleoverlay-{n}.png", true, HasSliders),
        new SkinElement("hitcircleselect.png", false)
    );

    private static readonly SkinSet SlidertrackSet = new(
        "Slidertrack",
        new SkinElement("sliderb{n}.png", true, HasSliders),
        new SkinElement("sliderfollowcircle-{n}.png", true, HasSliders),
        new SkinElement("sliderscorepoint.png", true, HasSliders),
        new SkinElement("sliderb-nd.png", false, HasSliders),
        new SkinElement("sliderb-spec.png", false, HasSliders)
    );

    private static readonly SkinSet HitcircleNumberSet = new(
        "Hitcircle numbers",
        new SkinElement("default-0.png", true),
        new SkinElement("default-1.png", true),
        new SkinElement("default-2.png", true),
        new SkinElement("default-3.png", true),
        new SkinElement("default-4.png", true),
        new SkinElement("default-5.png", true),
        new SkinElement("default-6.png", true),
        new SkinElement("default-7.png", true),
        new SkinElement("default-8.png", true),
        new SkinElement("default-9.png", true)
    );

    private static readonly SkinSet[] Sets =
    [
        CursorSet,
        HitburstSet,
        HitcircleSet,
        SlidertrackSet,
        HitcircleNumberSet,
    ];

    // Hitburst pairs which the RC requires to be visually distinguishable, since they carry
    // different gameplay meaning (perfect-combo "geki"/"katu" variants of hit100 and hit300).
    private static readonly (string A, string B)[] HitburstPairsThatMustDiffer =
    [
        ("hit100-{n}.png", "hit100k-{n}.png"),
        ("hit300-{n}.png", "hit300g-{n}.png"),
        ("hit300-{n}.png", "hit300k-{n}.png"),
    ];

    // The old and new spinner styles are mutually exclusive; mixing exclusive elements from both
    // causes the old style to silently take priority in-game.
    private static readonly string[] SpinnerShared =
    [
        "spinner-approachcircle.png",
        "spinner-clear.png",
        "spinner-spin.png",
    ];

    private static readonly string[] SpinnerOldExclusive =
    [
        "spinner-background.png",
        "spinner-circle.png",
        "spinner-metre.png",
    ];

    private static readonly string[] SpinnerNewExclusive =
    [
        "spinner-bottom.png",
        "spinner-glow.png",
        "spinner-middle.png",
        "spinner-middle2.png",
        "spinner-top.png",
    ];

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Standard],
            Category = "Skinning",
            Message = "Incomplete or incorrect skin element sets.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Ensuring that beatmap-specific skin elements are complete sets, use appropriate file formats,
                    and follow the osu! ranking criteria's skinning rules."
                },
                {
                    "Reasoning",
                    @"
                    When skinning a gameplay element, complete sets of elements need to be skinned in order to
                    avoid conflicts between user-specific and beatmap-specific skins. If a required element of a
                    partially-skinned set is missing, the user-specific skin's version of that element may be
                    used instead, which can look inconsistent or even unreadable together with the rest of the
                    beatmap skin.

                    Hit100/hit300 must remain distinguishable from their geki/katu counterparts so players can tell
                    a perfect hit from a regular one, and a custom slider border colour must be defined whenever
                    hitcircle or slider elements are skinned, so the beatmap's colour scheme doesn't conflict with a
                    player's own skin.
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
                "Mixed Spinner Style",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "Both old (\"{0}\") and new (\"{1}\") spinner style elements are present; the old style will take priority.",
                    "old file",
                    "new file"
                ).WithCause(
                    "Elements exclusive to both the old and new spinner styles are present at the same time."
                )
            },
            {
                "Duplicate Hitburst",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" and \"{1}\" are identical; hit100/hit300 hitbursts must be visually distinguishable from their geki/katu counterparts.",
                    "path",
                    "path"
                ).WithCause(
                    "hit100 or hit300 shares the exact same file as its corresponding katu/geki hitburst."
                )
            },
            {
                "Missing Slider Border",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0} skins hitcircle or slider elements but does not define a custom slider border colour (SliderBorder under [Colours]).",
                    "difficulty"
                ).WithCause(
                    "A custom slider border colour must be selected when a beatmap contains skin elements from the hit circle or slider sets."
                )
            },
            {
                "Similar Slider Colours",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "{0}'s slider body colour (SliderTrackOverride) is too similar to its slider border colour (SliderBorder).",
                    "difficulty"
                ).WithCause(
                    "The slider body colour is too close to the slider border colour, making the border lose its purpose as a visual boundary."
                )
            },
            {
                "New Spinner Style",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "\"{0}\": usage of the new style spinner is discouraged; it only displays correctly if the player's \"Preferred Skin\" is set to Default.",
                    "path"
                ).WithCause(
                    "Elements exclusive to the new spinner style are used, which is only recommended when a default skin is forced."
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

        foreach (var issue in GetSpinnerStyleIssues(beatmapSet))
            yield return issue;

        foreach (var issue in GetHitburstDistinctionIssues(beatmapSet))
            yield return issue;

        foreach (var issue in GetSliderColourIssues(beatmapSet))
            yield return issue;
    }

    private IEnumerable<Issue> GetSetIssues(BeatmapSet beatmapSet)
    {
        foreach (var set in Sets)
        {
            if (!SkinSetUtils.IsSetSkinned(set, beatmapSet))
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

    private IEnumerable<Issue> GetSpinnerStyleIssues(BeatmapSet beatmapSet)
    {
        if (!HasSpinners(beatmapSet))
            yield break;

        var oldFile = FindFirst(SpinnerOldExclusive, beatmapSet);
        var newFile = FindFirst(SpinnerNewExclusive, beatmapSet);

        if (oldFile != null && newFile != null)
        {
            yield return new Issue(
                GetTemplate("Mixed Spinner Style"),
                null,
                PathStatic.CutPath(oldFile),
                PathStatic.CutPath(newFile)
            );

            yield break;
        }

        if (newFile != null)
            yield return new Issue(
                GetTemplate("New Spinner Style"),
                null,
                PathStatic.CutPath(newFile)
            );

        var exclusive =
            oldFile != null ? SpinnerOldExclusive
            : newFile != null ? SpinnerNewExclusive
            : null;

        if (exclusive == null)
            yield break;

        var required = exclusive.Concat(SpinnerShared).ToArray();
        var missingSpinnerElements = required
            .Where(name =>
                !beatmapSet.SongFilePaths.Any(path =>
                    string.Equals(
                        PathStatic.CutPath(path),
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .ToList();

        if (missingSpinnerElements.Count > 0)
            yield return new Issue(
                GetTemplate("Incomplete Set"),
                null,
                "Spinner",
                string.Join(", ", missingSpinnerElements)
            );
    }

    private IEnumerable<Issue> GetHitburstDistinctionIssues(BeatmapSet beatmapSet)
    {
        foreach (var (nameA, nameB) in HitburstPairsThatMustDiffer)
        {
            var pathA = SkinSetUtils.GetPresentPath(new SkinElement(nameA, false), beatmapSet);
            var pathB = SkinSetUtils.GetPresentPath(new SkinElement(nameB, false), beatmapSet);

            if (pathA == null || pathB == null || !FilesAreIdentical(pathA, pathB))
                continue;

            yield return new Issue(
                GetTemplate("Duplicate Hitburst"),
                null,
                PathStatic.CutPath(pathA),
                PathStatic.CutPath(pathB)
            );
        }
    }

    private IEnumerable<Issue> GetSliderColourIssues(BeatmapSet beatmapSet)
    {
        var hitcircleOrSliderSkinned =
            SkinSetUtils.IsSetSkinnedAndRelevant(HitcircleSet, beatmapSet)
            || SkinSetUtils.IsSetSkinnedAndRelevant(SlidertrackSet, beatmapSet);

        if (!hitcircleOrSliderSkinned)
            yield break;

        foreach (
            var beatmap in beatmapSet.Beatmaps.Where(beatmap =>
                beatmap.GeneralSettings.mode == Beatmap.Mode.Standard
            )
        )
        {
            if (beatmap.ColourSettings.sliderBorder == null)
            {
                yield return new Issue(
                    GetTemplate("Missing Slider Border"),
                    null,
                    beatmap.MetadataSettings.version
                );

                continue;
            }

            if (beatmap.ColourSettings.sliderTrackOverride is not { } trackColour)
                continue;

            var distance = Vector3.Distance(trackColour, beatmap.ColourSettings.sliderBorder.Value);

            if (distance < SliderColourSimilarityThreshold)
                yield return new Issue(
                    GetTemplate("Similar Slider Colours"),
                    null,
                    beatmap.MetadataSettings.version
                );
        }
    }

    private static string? FindFirst(string[] names, BeatmapSet beatmapSet) =>
        names
            .Select(name =>
                beatmapSet.SongFilePaths.FirstOrDefault(path =>
                    string.Equals(
                        PathStatic.CutPath(path),
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            .FirstOrDefault(path => path != null);

    private static bool FilesAreIdentical(string pathA, string pathB)
    {
        try
        {
            return File.ReadAllBytes(pathA).AsSpan().SequenceEqual(File.ReadAllBytes(pathB));
        }
        catch (IOException)
        {
            return false;
        }
    }
}
