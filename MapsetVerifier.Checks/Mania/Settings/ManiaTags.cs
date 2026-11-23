using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Mania.Settings;

/// <summary> Looks up whole tags, since something like "DP" is found inside plenty of unrelated words. </summary>
public static class ManiaTags
{
    private static readonly char[] Separators = [' ', '\t', ',', '"', '(', ')', '[', ']'];

    public static bool Has(Beatmap beatmap, string tag) =>
        beatmap
            .MetadataSettings.tags.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Contains(tag, StringComparer.OrdinalIgnoreCase);
}
