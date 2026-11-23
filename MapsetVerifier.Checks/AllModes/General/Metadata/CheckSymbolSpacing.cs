using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public partial class CheckSymbolSpacing : GeneralCheck
    {
        private const string GroupTemplate = "Group";
        private const string SymbolTemplate = "Symbol";

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Missing spaces around symbols.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out symbols in the artist or title that need a space next to them: symbols that group
                        part of the field, like *Song★Title★* instead of *Song ★Title★*, and romanised stars, hearts and
                        arrows between two words, like *Love<3Machine* instead of *Love <3 Machine*."
                    },
                    {
                        "Reasoning",
                        @"
                        Writing symbols the same way everywhere keeps words apart and makes the same song or artist
                        searchable across beatmaps.

                        Artists may use symbols in ways that don't suggest spaces, which is why these are warnings."
                    },
                    {
                        "Limitations",
                        @"
                        Only symbols next to plain latin letters and digits are looked at, since other scripts may not
                        use spaces. Dashes and tildes only count as a group when there are several words between them,
                        and brackets when what's inside looks like a separate part, because they're also common inside
                        names (*f(x)*, *Re-birth*, *Mada~mada~*). Romanised symbols only count between two different
                        words, where the second one starts with a capital letter."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    GroupTemplate,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Missing space around \"{0}\" in {1} \"{2}\", unless not grouping.",
                        "group",
                        "field",
                        "field content"
                    )
                        .WithCause(
                            "Symbols that group part of the artist or title are directly next to the word before or after the group."
                        )
                        .WithRule(RC.Metadata.Symbols_SymbolsUsedGroupPartTitle)
                },
                {
                    SymbolTemplate,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Missing spaces around \"{0}\" in {1} \"{2}\", unless intended.",
                        "symbol",
                        "field",
                        "field content"
                    )
                        .WithCause(
                            "A star, heart or arrow in the unicode field is romanised without spaces between two words."
                        )
                        .WithRule(RC.Metadata.Symbols_FollowingUnicodeSymbolSubsetsLeading)
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var fields = beatmapSet
                .Beatmaps.SelectMany(beatmap =>
                {
                    var metadata = beatmap.MetadataSettings;
                    return new[]
                    {
                        ("artist", metadata.artist, metadata.artistUnicode),
                        ("title", metadata.title, metadata.titleUnicode),
                    };
                })
                .Distinct();

            foreach (var (field, romanised, unicode) in fields)
            {
                if (FindGroup(romanised) is { } group)
                    yield return new Issue(
                        GetTemplate(GroupTemplate),
                        null,
                        group,
                        $"romanised {field}",
                        romanised
                    );
                else if (FindSymbol(romanised, unicode) is { } symbol)
                    yield return new Issue(
                        GetTemplate(SymbolTemplate),
                        null,
                        symbol,
                        $"romanised {field}",
                        romanised
                    );

                if (unicode != romanised && FindGroup(unicode) is { } unicodeGroup)
                    yield return new Issue(
                        GetTemplate(GroupTemplate),
                        null,
                        unicodeGroup,
                        $"unicode {field}",
                        unicode
                    );
            }
        }

        /// <summary> The first group in the field that touches the word next to it, or null if there is none. </summary>
        private static string? FindGroup(string field)
        {
            var match = GroupAfterWord().Match(field);
            if (!match.Success)
                match = GroupBeforeWord().Match(field);

            if (match.Success)
            {
                var symbol = match.Groups["symbol"].Value[0];
                var content = match.Groups["content"].Value;
                var word = match.Groups["word"].Value;

                // More of the same symbol means it can't be told which of them belong together.
                var isPair = field.Count(ch => ch == symbol) == 2;
                // Dashes join words and tildes draw out a sound, so one word between them isn't clearly a group.
                var isClear = symbol is not ('-' or '~' or '～' or '〜') || content.Contains(' ');
                // "Love*Love*" repeats a word with the symbol behind it.
                var isRepeat = word.Equals(content, StringComparison.OrdinalIgnoreCase);

                if (isPair && isClear && !isRepeat)
                    return symbol + content + symbol;
            }

            foreach (Match bracket in BracketsAfterWord().Matches(field))
            {
                var content = bracket.Groups["content"].Value;

                // The marker spacing check reports these.
                if (MarkerStart().IsMatch(content))
                    continue;

                // Part of a stylised name otherwise, like "LOVE(LESS)" or "Eye(sight)".
                var isSeparatePart =
                    content.Contains(' ')
                    || (
                        char.IsAsciiLetterLower(bracket.Groups["word"].Value[^1])
                        && char.IsAsciiLetterUpper(content[0])
                    );
                if (isSeparatePart)
                    return bracket.Groups["group"].Value;
            }

            return null;
        }

        /// <summary>
        ///     The first symbol sitting between two words of the romanised field without any spaces, which is how a
        ///     star, heart or arrow of the unicode field was romanised. Null if there is none.
        /// </summary>
        private static string? FindSymbol(string romanised, string unicode)
        {
            foreach (Match match in SymbolBetweenWords().Matches(romanised))
            {
                // "Kira*Kira" decorates a repeated word.
                if (
                    match
                        .Groups["word"]
                        .Value.Equals(
                            match.Groups["next"].Value,
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                    continue;

                var symbol = match.Groups["symbol"].Value;
                var isFromUnicode = symbol switch
                {
                    "*" => StarSymbols().IsMatch(unicode),
                    "<3" => HeartSymbols().IsMatch(unicode),
                    _ => ArrowSymbols().IsMatch(unicode),
                };

                if (isFromUnicode)
                    return symbol;
            }

            return null;
        }

        [GeneratedRegex(
            @"(?<word>[A-Za-z0-9]{2,})(?<symbol>[★☆✪✰♥♡❤♪*~～〜-])(?<content>[A-Za-z0-9](?:(?!\k<symbol>).)*[A-Za-z0-9.!?])\k<symbol>(?=\s|$)"
        )]
        private static partial Regex GroupAfterWord();

        [GeneratedRegex(
            @"(?<=\s|^)(?<symbol>[★☆✪✰♥♡❤♪*~～〜-])(?<content>[A-Za-z0-9](?:(?!\k<symbol>).)*[A-Za-z0-9.!?])\k<symbol>(?<word>[A-Za-z0-9]{2,})"
        )]
        private static partial Regex GroupBeforeWord();

        [GeneratedRegex(
            @"(?<word>[A-Za-z0-9]{2,})(?<group>\((?<content>[^()]{3,})\)|\[(?<content>[^\[\]]{3,})\])(?=\s|$)"
        )]
        private static partial Regex BracketsAfterWord();

        [GeneratedRegex(@"^(?:CV|feat|ft)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MarkerStart();

        // A capital letter after the symbol starts a new word; otherwise it may replace a letter, like "Sh★tty".
        [GeneratedRegex(
            @"(?<word>[A-Za-z0-9]{2,})(?<symbol>\*|<3|-+>|<-+)(?=(?<next>[A-Z][A-Za-z]+))"
        )]
        private static partial Regex SymbolBetweenWords();

        // Among Miscellaneous Symbols and Dingbats.
        [GeneratedRegex("[★☆⚝✡-✧✩-❋]")]
        private static partial Regex StarSymbols();

        [GeneratedRegex("[♡♥❣-❧]")]
        private static partial Regex HeartSymbols();

        // The arrows among Dingbats, Supplemental Arrows-A and -B, and Miscellaneous Symbols and Arrows. Plain
        // arrows (→) are in another set, which the rule leaves to be handled case by case.
        [GeneratedRegex("[➔➘-➯➱-➾⟰-⟿⤀-⥿⬀-⬑]")]
        private static partial Regex ArrowSymbols();
    }
}
