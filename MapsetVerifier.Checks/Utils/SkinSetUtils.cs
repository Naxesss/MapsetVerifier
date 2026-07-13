using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Utils;

/// <summary> A single skin element within a <see cref="SkinSet" />, e.g. "cursor.png" or "hit0-{n}.png". </summary>
public class SkinElement(
    string pattern,
    bool required,
    Func<BeatmapSet, bool>? usedCondition = null
)
{
    public string Pattern { get; } = pattern;
    public bool Required { get; } = required;

    /// <summary> Whether this element is relevant for the given mapset, by default always. </summary>
    public Func<BeatmapSet, bool> UsedCondition { get; } = usedCondition ?? (_ => true);
}

/// <summary>
///     A group of skin elements which must be skinned in their entirety to avoid conflicts between
///     user-specific and beatmap-specific skins, per the "complete sets" skinning rule.
/// </summary>
public class SkinSet(string name, params SkinElement[] elements)
{
    public string Name { get; } = name;
    public SkinElement[] Elements { get; } = elements;
}

public static class SkinSetUtils
{
    /// <summary> Returns the path of a file in the mapset matching the given element's pattern, if any. </summary>
    public static string? GetPresentPath(SkinElement element, BeatmapSet beatmapSet) =>
        beatmapSet.SongFilePaths.FirstOrDefault(path =>
            MatchesPattern(PathStatic.CutPath(path), element.Pattern)
        );

    /// <summary> Returns every file in the mapset matching the given element's pattern (e.g. all animation frames). </summary>
    public static IEnumerable<string> GetAllPresentPaths(
        SkinElement element,
        BeatmapSet beatmapSet
    ) =>
        beatmapSet.SongFilePaths.Where(path =>
            MatchesPattern(PathStatic.CutPath(path), element.Pattern)
        );

    public static bool IsElementPresent(SkinElement element, BeatmapSet beatmapSet) =>
        GetPresentPath(element, beatmapSet) != null;

    /// <summary> Whether any element (required or optional) of the set is present in the mapset. </summary>
    public static bool IsSetSkinned(SkinSet set, BeatmapSet beatmapSet) =>
        set.Elements.Any(element => IsElementPresent(element, beatmapSet));

    /// <summary>
    ///     Whether any element (required or optional) of the set is both present in the mapset and
    ///     relevant to it (see <see cref="SkinElement.UsedCondition" />). Unlike <see cref="IsSetSkinned" />,
    ///     this ignores stray files that don't apply to anything in the mapset (e.g. a leftover
    ///     "sliderb.png" in a mapset with no sliders at all).
    /// </summary>
    public static bool IsSetSkinnedAndRelevant(SkinSet set, BeatmapSet beatmapSet) =>
        set.Elements.Any(element =>
            element.UsedCondition(beatmapSet) && IsElementPresent(element, beatmapSet)
        );

    /// <summary>
    ///     The required elements of the set that are both relevant to the mapset (see
    ///     <see cref="SkinElement.UsedCondition" />) and missing from it.
    /// </summary>
    public static IEnumerable<SkinElement> GetMissingRequiredElements(
        SkinSet set,
        BeatmapSet beatmapSet
    ) =>
        set.Elements.Where(element =>
            element.Required
            && element.UsedCondition(beatmapSet)
            && !IsElementPresent(element, beatmapSet)
        );

    /// <summary>
    ///     Matches a file name against a skin element pattern, ignoring extension (osu! skin images
    ///     may be .png or .jpg regardless of what the pattern specifies). Patterns may contain a
    ///     "-{n}" or "{n}" token (e.g. "hit0-{n}.png", "sliderb{n}.png"), which can either be replaced
    ///     with nothing (a single static image) or with a run of digits (an animation frame), per the
    ///     skinning naming convention.
    /// </summary>
    private static bool MatchesPattern(string fileName, string pattern)
    {
        var fileBase = StripExtension(fileName);
        var patternBase = StripExtension(pattern);

        if (string.Equals(fileBase, patternBase, StringComparison.OrdinalIgnoreCase))
            return true;

        var token =
            patternBase.Contains("-{n}") ? "-{n}"
            : patternBase.Contains("{n}") ? "{n}"
            : null;

        if (token == null)
            return false;

        var tokenIndex = patternBase.IndexOf(token, StringComparison.Ordinal);
        var prefix = patternBase[..tokenIndex];
        var suffix = patternBase[(tokenIndex + token.Length)..];

        if (string.Equals(fileBase, prefix + suffix, StringComparison.OrdinalIgnoreCase))
            return true;

        var numberedPrefix = token == "-{n}" ? prefix + "-" : prefix;

        if (
            !fileBase.StartsWith(numberedPrefix, StringComparison.OrdinalIgnoreCase)
            || !fileBase.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            || fileBase.Length <= numberedPrefix.Length + suffix.Length
        )
            return false;

        var middle = fileBase[numberedPrefix.Length..^suffix.Length];

        return middle.Length > 0 && middle.All(char.IsDigit);
    }

    private static string StripExtension(string name)
    {
        var dotIndex = name.LastIndexOf('.');

        return dotIndex >= 0 ? name[..dotIndex] : name;
    }
}
