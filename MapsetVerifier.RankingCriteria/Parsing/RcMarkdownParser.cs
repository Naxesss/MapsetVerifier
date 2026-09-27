using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria.Model;

namespace MapsetVerifier.RankingCriteria.Parsing;

/// <summary> A statement as found in the markdown, before it is matched to an id in the catalogue. </summary>
public sealed class ParsedStatement
{
    public RcUpstream Upstream { get; init; } = new();

    /// <summary> Index of the parent statement in the parsed list, if this is a nested statement. </summary>
    public int? ParentIndex { get; init; }

    /// <summary> Last non-kind heading above the statement, used for drafting ids. </summary>
    public string Section { get; init; } = "";
}

/// <summary>
///     Extracts rules, guidelines and allowances from a ranking criteria page. Statements are list items (or paragraphs)
///     starting with bold text below a "Rules", "Guidelines" or "Allowances" heading; nested bold list items become
///     child statements.
/// </summary>
public static partial class RcMarkdownParser
{
    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<text>.*?)\s*#*\s*$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(?<indent>\s*)(?:[-*+]|\d+\.)\s+(?<text>.*)$")]
    private static partial Regex ListItemRegex();

    [GeneratedRegex(@"^\*\*(?<bold>.+?)\*\*(?<rest>.*)$")]
    private static partial Regex LeadingBoldRegex();

    [GeneratedRegex(@"^(?<sentence>.*?[.!?:])(\s|$)")]
    private static partial Regex FirstSentenceRegex();

    /// <summary> Like <see cref="FirstSentenceRegex" />, but a colon or an abbreviation such as "etc." does not end it. </summary>
    [GeneratedRegex(@"^(?<sentence>.*?(?<!\b(?:etc|e\.g|i\.e|vs|Ver|ft|feat))[.!?])(\s|$)")]
    private static partial Regex PlainSentenceRegex();

    private sealed record Heading(int Level, string Text);

    private sealed record OpenItem(int Indent, int Index);

    public static List<ParsedStatement> Parse(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var statements = new List<ParsedStatement>();
        var startLines = new List<int>();
        var ownEnds = new List<int>();
        var headings = new List<Heading>();
        var openItems = new List<OpenItem>();
        var inCodeBlock = false;
        var inComment = false;

        for (var i = 0; i < lines.Length; ++i)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            if (line.TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
                continue;

            // Multi-line HTML comments are maintainer notes, not content.
            if (inComment)
            {
                if (line.Contains("-->"))
                    inComment = false;
                continue;
            }

            if (line.TrimStart().StartsWith("<!--") && !line.Contains("-->"))
            {
                inComment = true;
                continue;
            }

            var headingMatch = HeadingRegex().Match(line);
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups["hashes"].Length;
                headings.RemoveAll(heading => heading.Level >= level);
                headings.Add(new Heading(level, headingMatch.Groups["text"].Value));
                openItems.Clear();
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var indent = line.Length - line.TrimStart().Length;
            var listMatch = ListItemRegex().Match(line);
            var isListItem = listMatch.Success;
            var content = isListItem ? listMatch.Groups["text"].Value : line.Trim();

            // A line which is neither a list item nor indented ends every open list.
            if (!isListItem && indent == 0)
                openItems.Clear();

            if (isListItem)
            {
                openItems.RemoveAll(item => item.Indent >= indent);
            }
            else
            {
                // Continuation text belongs to the innermost open statement.
                if (openItems.Count > 0 && indent > 0)
                    ownEnds[openItems[^1].Index] = lineNumber;

                if (indent > 0)
                    continue;
            }

            var kindHeadingIndex = headings.FindLastIndex(heading =>
                ParseKind(heading.Text) != null
            );
            if (kindHeadingIndex < 0)
                continue;

            var boldMatch = LeadingBoldRegex().Match(content);
            if (!boldMatch.Success)
            {
                // Plain list items nested in a statement (examples, explanations) are part of it.
                if (isListItem && openItems.Count > 0)
                {
                    ownEnds[openItems[^1].Index] = lineNumber;
                    continue;
                }

                // Plain paragraphs are prose; plain top-level list items are statements too, such as the
                // difficulty setting guidelines and marker lists like "- `(TV Size)`".
                if (!isListItem)
                    continue;
            }

            var kind = ParseKind(headings[kindHeadingIndex].Text)!.Value;
            // Skip the page title and nested kind headings such as "Rules" above "Rules: Markers you must add".
            var sectionHeadings = headings
                .Take(kindHeadingIndex)
                .Skip(1)
                .Where(heading => ParseKind(heading.Text) == null)
                .ToList();
            var parentIndex = isListItem && openItems.Count > 0 ? openItems[^1].Index : (int?)null;

            statements.Add(
                new ParsedStatement
                {
                    ParentIndex = parentIndex,
                    Section =
                        sectionHeadings.Count > 0
                            ? RcText.HeadingText(sectionHeadings[^1].Text)
                            : "",
                    Upstream = new RcUpstream
                    {
                        Path = sectionHeadings
                            .Select(heading => RcText.HeadingText(heading.Text))
                            .ToList(),
                        Kind = kind,
                        Lead = boldMatch.Success
                            ? Lead(boldMatch.Groups["bold"].Value, boldMatch.Groups["rest"].Value)
                            : PlainLead(content),
                        Anchor =
                            sectionHeadings.Count > 0
                                ? RcText.Anchor(sectionHeadings[^1].Text)
                                : "",
                        Difficulties = ParseDifficulties(headings),
                    },
                }
            );
            startLines.Add(lineNumber);
            ownEnds.Add(lineNumber);

            if (isListItem)
                openItems.Add(new OpenItem(indent, statements.Count - 1));
        }

        for (var index = 0; index < statements.Count; ++index)
        {
            var upstream = statements[index].Upstream;
            upstream.StartLine = startLines[index];
            upstream.EndLine = ownEnds[index];
            upstream.Lead = CompleteShortLead(
                upstream.Lead,
                lines,
                upstream.StartLine,
                upstream.EndLine
            );
            upstream.Fingerprint = RcText.Fingerprint(upstream.Lead);

            var body = string.Join("\n", lines[(upstream.StartLine - 1)..upstream.EndLine]);
            upstream.BodyHash = RcText.Hash(RcText.StripMarkdown(body));
        }

        for (var index = 0; index < statements.Count; ++index)
        {
            var hasChildren = statements.Any(statement => statement.ParentIndex == index);
            statements[index].Upstream.Intro =
                hasChildren && IsIncompleteSentence(statements[index].Upstream.Lead);
        }

        return statements;
    }

    /// <summary> "The audio file of a beatmap must..." or "Tags must include the following when applicable:". </summary>
    private static bool IsIncompleteSentence(string lead)
    {
        var text = lead.TrimEnd();
        return text.EndsWith("...") || text.EndsWith('…') || text.EndsWith(':');
    }

    /// <summary>
    ///     The bold part of a statement, extended to the end of its sentence when the bold part is only a fragment, as in
    ///     "**...lower than 2:30**, the lowest difficulty cannot be harder than a Normal."
    /// </summary>
    private static string Lead(string bold, string rest)
    {
        var boldText = RcText.StripMarkdown(bold);
        var restText = RcText.StripMarkdown(rest);

        if (boldText.Length > 0 && ".!?".Contains(boldText[^1]))
            return boldText;

        // "**Minimum width:** 160px" is only meaningful together with its value.
        if (boldText.EndsWith(':') && restText.Length == 0)
            return boldText;

        var sentence = FirstSentenceRegex().Match(restText);
        var continuation = sentence.Success ? sentence.Groups["sentence"].Value : restText;
        var separator = continuation.StartsWith(',') || continuation.StartsWith('.') ? "" : " ";

        return (boldText + separator + continuation).Trim();
    }

    /// <summary> The first sentence of a statement without bold text, e.g. "Approach rate should be 6 or lower." </summary>
    private static string PlainLead(string content)
    {
        var text = RcText.StripMarkdown(content);
        var sentence = PlainSentenceRegex().Match(text);

        return sentence.Success && sentence.Groups["sentence"].Value.Length < text.Length
            ? sentence.Groups["sentence"].Value
            : text;
    }

    /// <summary>
    ///     Leads such as "Japanese", "`vs.`" or "`(TV Size)`" only name what the statement is about; the rule itself is
    ///     on the next line, so that line is appended: "Japanese: Use the Modified Hepburn system".
    /// </summary>
    private static string CompleteShortLead(string lead, string[] lines, int startLine, int endLine)
    {
        var words = RcText
            .Fingerprint(lead)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Length;
        if (words > 3 || endLine <= startLine)
            return lead;

        for (var line = startLine; line < endLine; ++line)
        {
            var text = ListItemRegex().Match(lines[line]) is { Success: true } item
                ? item.Groups["text"].Value
                : lines[line];
            var next = PlainLead(text);

            if (next.Length > 0)
                return $"{lead.TrimEnd(':')}: {next}";
        }

        return lead;
    }

    /// <summary> "Rules", "Rules: Markers", "Guidelines", "Difficulty setting guidelines", "Allowances", ... </summary>
    private static RcKind? ParseKind(string heading)
    {
        var text = RcText.HeadingText(heading).ToLowerInvariant();

        if (text.StartsWith("rules"))
            return RcKind.Rule;

        if (text.StartsWith("guidelines") || text.EndsWith("guidelines"))
            return RcKind.Guideline;

        if (text.StartsWith("allowances"))
            return RcKind.Allowance;

        return null;
    }

    private static List<Beatmap.Difficulty> ParseDifficulties(List<Heading> headings)
    {
        for (var i = headings.Count - 1; i >= 0; --i)
        {
            var difficulty = RcText.DifficultyFromIcon(headings[i].Text);
            if (difficulty == null)
                continue;

            return difficulty switch
            {
                "easy" => [Beatmap.Difficulty.Easy],
                "normal" => [Beatmap.Difficulty.Normal],
                "hard" => [Beatmap.Difficulty.Hard],
                "insane" => [Beatmap.Difficulty.Insane],
                // The wiki's expert icon also covers everything above Expert.
                "expert" or "extra" => [Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra],
                _ => [],
            };
        }

        return [];
    }
}
