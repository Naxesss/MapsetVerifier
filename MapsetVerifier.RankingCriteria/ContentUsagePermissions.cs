using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.RankingCriteria.Parsing;

namespace MapsetVerifier.RankingCriteria;

public enum ContentUsageStatus
{
    /// <summary> Only some of the artist's songs may be used; <see cref="ContentUsageArtist.Notes" /> says which. </summary>
    AllowedWithExceptions,

    /// <summary> The artist does not allow their songs to be used without explicit permission. </summary>
    Disallowed,
}

/// <param name="Name"> The artist as the wiki lists them, e.g. "Yuyoyuppe / DJ'TEKINA//SOMETHING". </param>
/// <param name="Aliases"> The names to look for in metadata, e.g. "Yuyoyuppe" and "DJ'TEKINA//SOMETHING". </param>
/// <param name="Notes"> What is and is not allowed, as plain text. Only set for artists allowed with exceptions. </param>
public sealed record ContentUsageArtist(
    string Name,
    IReadOnlyList<string> Aliases,
    ContentUsageStatus Status,
    string? Notes
);

/// <summary>
///     The artists from the osu! wiki's content usage permissions page who restrict the use of their songs, read
///     from the snapshot embedded in this assembly. Artists that allow everything are left out, since there is
///     nothing to warn about for them.
/// </summary>
public static partial class ContentUsagePermissions
{
    public const string PageKey = "content-usage";

    private static readonly Lazy<IReadOnlyList<ContentUsageArtist>> LazyRestricted = new(() =>
        Parse(RcStore.Embedded.GetMarkdown(PageKey) ?? "")
    );

    public static IReadOnlyList<ContentUsageArtist> Restricted => LazyRestricted.Value;

    public static string WikiUrl => RcPages.Get(PageKey)?.WikiUrl + "#artist-permissions";

    public static IReadOnlyList<ContentUsageArtist> Parse(string markdown)
    {
        var artists = new List<ContentUsageArtist>();

        foreach (var line in markdown.Replace("\r", "").Split('\n'))
        {
            if (!line.StartsWith('|'))
                continue;

            var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

            // The status is an image like ![][partial]; header and alignment rows have none.
            var statusIndex = cells.FindIndex(cell => StatusRegex().IsMatch(cell));
            if (statusIndex < 1)
                continue;

            var status = StatusRegex().Match(cells[statusIndex]).Groups["status"].Value switch
            {
                "partial" => ContentUsageStatus.AllowedWithExceptions,
                "false" => ContentUsageStatus.Disallowed,
                _ => (ContentUsageStatus?)null,
            };

            if (status == null)
                continue;

            var name = Unescape(RcText.StripMarkdown(cells[statusIndex - 1]));
            var notes =
                status == ContentUsageStatus.AllowedWithExceptions && statusIndex + 1 < cells.Count
                    ? Unescape(RcText.StripMarkdown(cells[statusIndex + 1]))
                    : null;

            if (name.Length > 0)
                artists.Add(new ContentUsageArtist(name, GetAliases(name), status.Value, notes));
        }

        return artists;
    }

    /// <summary> Returns the restricted artists credited in the given metadata, e.g. an artist or source field. </summary>
    public static IEnumerable<ContentUsageArtist> FindIn(
        IReadOnlyList<ContentUsageArtist> artists,
        params string?[] fields
    )
    {
        var normalizedFields = fields
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .Select(field => Normalize(field!))
            .ToList();

        return artists.Where(artist =>
            artist.Aliases.Any(alias =>
                normalizedFields.Any(field => ContainsWord(field, Normalize(alias)))
            )
        );
    }

    /// <summary>
    ///     "Mlumìn // SoundWarper" is two artists and "Yuyoyuppe / DJ'TEKINA//SOMETHING" one artist under two names;
    ///     either way each part is a name of its own. A note in brackets, like "MEGAREX (music label)", is not.
    /// </summary>
    private static List<string> GetAliases(string name) =>
        AliasSeparatorRegex()
            .Split(name)
            .Select(part => BracketRegex().Replace(part, "").Trim())
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary> Whether the name appears in the text without being part of a longer word, like "NOMA" in "NOMAD". </summary>
    private static bool ContainsWord(string text, string name)
    {
        for (
            var index = text.IndexOf(name, StringComparison.Ordinal);
            index >= 0;
            index = text.IndexOf(name, index + 1, StringComparison.Ordinal)
        )
        {
            var before = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var end = index + name.Length;
            var after = end == text.Length || !char.IsLetterOrDigit(text[end]);

            if (before && after)
                return true;
        }

        return false;
    }

    /// <summary> Lower case without accents, so "Mlumin" matches "Mlumìn". </summary>
    private static string Normalize(string text)
    {
        var builder = new StringBuilder();

        foreach (var c in text.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string Unescape(string text) => EscapeRegex().Replace(text, "$1");

    [GeneratedRegex(@"!\[\]\[(?<status>true|false|partial)\]")]
    private static partial Regex StatusRegex();

    [GeneratedRegex(@"\s+/{1,2}\s+")]
    private static partial Regex AliasSeparatorRegex();

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex BracketRegex();

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex EscapeRegex();
}
