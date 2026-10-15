using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MapsetVerifier.RankingCriteria.Parsing;

/// <summary> Text helpers for turning wiki markdown into comparable and displayable strings. </summary>
public static partial class RcText
{
    private static readonly HashSet<string> SlugStopWords =
    [
        "a",
        "an",
        "the",
        "of",
        "to",
        "in",
        "on",
        "for",
        "and",
        "or",
        "be",
        "is",
        "are",
        "must",
        "should",
        "can",
        "cannot",
        "may",
        "with",
        "by",
        "as",
        "at",
        "that",
        "this",
        "which",
        "it",
        "its",
        "their",
        "from",
        "if",
        "all",
        "any",
        "each",
        "every",
        "your",
        "you",
        "s",
        "has",
        "have",
        "been",
        "than",
        "there",
        "do",
        "into",
        "so",
        "they",
        "them",
        "when",
        "such",
    ];

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex HtmlCommentRegex();

    [GeneratedRegex(@"\[\^[^\]]+\]")]
    private static partial Regex FootnoteRegex();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"/wiki/shared/diff/(?<difficulty>[a-z]+)-(?<mode>[otcm])\.png")]
    private static partial Regex DifficultyIconRegex();

    /// <summary> Removes comments, footnote references, images, links and emphasis, keeping inline code. </summary>
    public static string StripMarkdown(string markdown)
    {
        var text = HtmlCommentRegex().Replace(markdown, "");
        text = FootnoteRegex().Replace(text, "");
        text = ImageRegex().Replace(text, "");
        text = LinkRegex().Replace(text, "$1");
        text = text.Replace("**", "").Replace("*", "");
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    /// <summary> Lower-cased words only, so formatting and punctuation changes do not count as rewording. </summary>
    public static string Fingerprint(string markdown)
    {
        var text = StripMarkdown(markdown).Replace("`", "").ToLowerInvariant();

        return NonWordRegex().Replace(text, " ").Trim();
    }

    /// <summary> Word overlap between two fingerprints, from 0 (nothing shared) to 1 (same words). </summary>
    public static double Similarity(string fingerprintA, string fingerprintB)
    {
        var a = fingerprintA.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var b = fingerprintB.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (a.Count == 0 && b.Count == 0)
            return 1;

        var union = a.Union(b).Count();

        return union == 0 ? 0 : (double)a.Intersect(b).Count() / union;
    }

    /// <summary> Draft slug from the significant words of a lead, e.g. "hit-objects-never-off-screen-4". </summary>
    public static string Slug(string text, int maxWords = 5)
    {
        // Ids stay ASCII: "ø" and "ß" are dropped, accented letters lose their accent.
        var ascii = new string(
            Fingerprint(text)
                .Normalize(NormalizationForm.FormD)
                .Where(c => c is ' ' or (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                .ToArray()
        );
        var words = ascii
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !SlugStopWords.Contains(word))
            .Take(maxWords);

        var slug = string.Join("-", words);

        return slug.Length == 0 ? "statement" : slug;
    }

    /// <summary> Heading text as shown to a reader: no images, links or emphasis. </summary>
    public static string HeadingText(string heading) => StripMarkdown(heading).Replace("`", "");

    /// <summary> Anchor osu-web generates for a heading: lower-cased, words joined by dashes. </summary>
    public static string Anchor(string heading)
    {
        var text = HeadingText(heading).ToLowerInvariant();

        return NonWordRegex().Replace(text, "-").Trim('-');
    }

    /// <summary> Parses a difficulty icon such as "/wiki/shared/diff/normal-c.png" into its difficulty name. </summary>
    public static string? DifficultyFromIcon(string markdown)
    {
        var match = DifficultyIconRegex().Match(markdown);

        return match.Success ? match.Groups["difficulty"].Value : null;
    }

    public static string PascalCase(string slug)
    {
        var builder = new StringBuilder();

        foreach (var part in NonWordRegex().Split(slug))
        {
            if (part.Length == 0)
                continue;

            builder.Append(char.ToUpperInvariant(part[0]));
            builder.Append(part[1..]);
        }

        return builder.ToString();
    }

    public static string Hash(string text)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(WhitespaceRegex().Replace(text, " ").Trim())
        );

        return Convert.ToHexStringLower(bytes)[..16];
    }
}
