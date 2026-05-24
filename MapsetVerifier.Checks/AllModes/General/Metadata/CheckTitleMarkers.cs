using System.Globalization;
using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckTitleMarkers : GeneralCheck
    {
        // Most specific patterns first to avoid duplicate or incorrect suggestions.
        private static readonly MarkerFormat[] MarkerFormatsOrdered =
        [
            new(
                Marker.SPED_UP_CUT_VER,
                new Regex(@"(?i)(sped|speed)\s*up(\s+ver\.?)?\s*&\s*cut(\s+(size|ver(sion)?))?")
            ),
            new(
                Marker.NIGHTCORE_CUT_VER,
                new Regex(
                    @"(?i)(nightcore|night\s+core)(\s+(ver\.?|mix))?\s*&\s*cut(\s+(size|ver(sion)?))?"
                )
            ),
            new(Marker.TV_SIZE, new Regex(@"(?i)\btv\s+(size|ver(sion)?)\b")),
            new(Marker.EXTENDED_EDIT, new Regex(@"(?i)\bextended\s+edit\b")),
            new(
                Marker.MOVIE_VER,
                new Regex(@"(?i)\b(movie\s+(size|ver(sion)?|edit|cut)|movie\s+version)\b")
            ),
            new(
                Marker.GAME_VER,
                new Regex(@"(?i)\b(game\s+(size|ver(sion)?)|game\s+op\s+edit|~game\s+size~)\b")
            ),
            new(
                Marker.SHORT_VER,
                new Regex(@"(?i)(~?\s*-?\s*short(\s+(size|ver(sion)?))?~?|\(short\)|-short\s*ver-)")
            ),
            new(Marker.CUT_VER, new Regex(@"(?i)(?<!&\s)\bcut\s+(size|ver(sion)?)\b")),
            new(
                Marker.SPED_UP_VER,
                new Regex(@"(?i)(?<!&\s)\b(sped|speed)\s*up(\s+ver(sion)?)?\b")
            ),
            new(
                Marker.NIGHTCORE_MIX,
                new Regex(@"(?i)(?<!&\s)\b(nightcore|night\s+core)\s+(ver\.?|mix)\b")
            ),
        ];

        private static readonly Regex GenericVersionMarkerRegex = new(
            @"\(([^)]+)\s+Version\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex NonCanonicalParentheticalVerRegex = new(
            @"\(([^)]+)\s+ver\.?\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex TrailingUnparenthesizedVerRegex = new(
            @"\s([^\s(]+)\s+ver\.?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex LongMarkerRegex = new(
            @"\(Long\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex OpVersionRegex = new(
            @"\bOP\s+Version\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex LiveMarkerRegex = new(
            @"\([^)]*\blive\b[^)]*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly IEnumerable<TitleType> TitleTypes =
        [
            new("romanized", beatmap => beatmap.MetadataSettings.title),
            new("unicode", beatmap => beatmap.MetadataSettings.titleUnicode),
        ];

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message =
                    "Incorrect format of title version markers such as (TV Size), (Game Ver.), (Extended Edit), etc.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Standardizing title version markers for ranked content per the markers ranking criteria.

                        ![](assets/checks/all-modes-general-metadata-title-markers-1.png ""A song using ""-TV version-"" as its official metadata, which becomes ""(TV Size)"" when standardized."")
                        "
                    },
                    {
                        "Reasoning",
                        @"
                        Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the
                        most part eliminated through standardization. Standardization also reduces confusion in case of
                        multiple correct ways to write certain fields and contributes to making metadata more consistent
                        across official content."
                    },
                    {
                        "Limitations",
                        @"
                        This check cannot verify whether a marker *must* be added (e.g. TV Size for a TV airing, or Cut Ver.
                        for a non-official cut), whether audio matches an official version, or whether Short/Game/Movie markers
                        should be added when only one version of a song exists. Custom combined markers approved after discussion
                        may still need manual review."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} title field; \"{1}\" incorrect format of \"{2}\".",
                        "Romanized/unicode",
                        "field",
                        "title marker"
                    ).WithCause(
                        @"The format of a title marker, in either the romanized or unicode title, is incorrect.
                        Standard markers include:
                        - (TV Size)
                        - (Game Ver.)
                        - (Short Ver.)
                        - (Movie Ver.)
                        - (Cut Ver.)
                        - (Extended Edit)
                        - (Sped Up Ver.)
                        - (Nightcore Mix)
                        - (Sped Up & Cut Ver.)
                        - (Nightcore & Cut Ver.)"
                    )
                },
                {
                    "Warning Nightcore Tag",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" in tags, consider \"{1}\" instead of \"{2}\" in {3} title.",
                        "nightcore",
                        "(Nightcore Mix)",
                        "(Sped Up Ver.)",
                        "romanized/unicode"
                    ).WithCause(
                        "The title contains a sped-up marker while tags indicate nightcore (pitch-up). Use a Nightcore marker instead."
                    )
                },
                {
                    "Warning Guideline Ver",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} title field; \"{1}\" should use guideline format \"{2}\".",
                        "Romanized/unicode",
                        "field",
                        "title marker"
                    ).WithCause(
                        @"Length or version markers not covered by the standard list should use a descriptive ""(#### Ver.)"" form in title case, e.g. ""(Extended Version)"" -> ""(Extended Ver.)"", ""(Long)"" -> ""(Long Ver.)""."
                    )
                },
                {
                    "Warning Tv Position",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} title field; \"{1}\" — (TV Size) should be at the end of the title.",
                        "Romanized/unicode",
                        "field"
                    ).WithCause(
                        "The TV Size marker should appear at the end of the title, typically as the final parenthetical."
                    )
                },
                {
                    "Warning Op Version",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} title field; \"{1}\" — consider \"(Game Ver.)\" instead of \"OP Version\" (game-related tags detected).",
                        "Romanized/unicode",
                        "field"
                    ).WithCause(
                        "\"OP Version\" in a game context should be standardized to (Game Ver.)."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (!beatmapSet.Beatmaps.Any())
                yield break;

            var beatmap = beatmapSet.Beatmaps[0];

            foreach (var issue in GetMarkerFormatIssues(beatmap))
                yield return issue;

            foreach (var issue in GetTvPositionIssues(beatmap))
                yield return issue;

            foreach (var issue in GetOpVersionIssues(beatmap))
                yield return issue;

            foreach (var issue in GetGuidelineVerIssues(beatmap))
                yield return issue;

            foreach (var issue in GetNightcoreIssues(beatmap))
                yield return issue;
        }

        private IEnumerable<Issue> GetMarkerFormatIssues(Beatmap beatmap)
        {
            foreach (var markerFormat in MarkerFormatsOrdered)
            foreach (var issue in GetIssuesFromMarkerFormat(beatmap, markerFormat))
                yield return issue;
        }

        private IEnumerable<Issue> GetIssuesFromMarkerFormat(
            Beatmap beatmap,
            MarkerFormat markerFormat
        )
        {
            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (titleType.type == "unicode" && title == beatmap.MetadataSettings.title)
                    continue;

                var correctFormat = markerFormat.marker.Value;
                var approxRegex = markerFormat.approxRegex;

                if (!approxRegex.IsMatch(title))
                    continue;

                if (ShouldSkipIndividualTempoMarker(markerFormat.marker, title))
                    continue;

                if (IsMarkerCasingAcceptable(title, correctFormat))
                    continue;

                if (
                    IsCombinedMarkerFormat(markerFormat)
                    && IsApproxMarkerSatisfied(title, markerFormat)
                )
                    continue;

                if (IsWithinLiveMarker(title, approxRegex))
                    continue;

                yield return new Issue(
                    GetTemplate("Problem"),
                    null,
                    Capitalize(titleType.type),
                    title,
                    correctFormat
                );
            }
        }

        private static readonly Regex TvApproxRegex = new(
            @"(?i)\btv\s+(size|ver(sion)?)\b",
            RegexOptions.Compiled
        );

        private static bool ShouldSkipIndividualTempoMarker(Marker marker, string title)
        {
            if (
                marker != Marker.SPED_UP_VER
                && marker != Marker.CUT_VER
                && marker != Marker.NIGHTCORE_MIX
            )
                return false;

            return IsCombinedTempoMarkerPresent(title);
        }

        private static bool IsCombinedMarkerFormat(MarkerFormat markerFormat) =>
            markerFormat.marker == Marker.SPED_UP_CUT_VER
            || markerFormat.marker == Marker.NIGHTCORE_CUT_VER;

        private static bool IsCombinedTempoMarkerPresent(string title) =>
            IsApproxMarkerSatisfied(title, MarkerFormatsOrdered[0])
            || IsApproxMarkerSatisfied(title, MarkerFormatsOrdered[1]);

        private IEnumerable<Issue> GetTvPositionIssues(Beatmap beatmap)
        {
            var tvApprox = TvApproxRegex;

            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                if (string.IsNullOrWhiteSpace(title) || !tvApprox.IsMatch(title))
                    continue;

                if (titleType.type == "unicode" && title == beatmap.MetadataSettings.title)
                    continue;

                var lastParenthetical = GetLastParenthetical(title);
                if (lastParenthetical != null && tvApprox.IsMatch(lastParenthetical))
                {
                    if (IsMarkerCasingAcceptable(title, Marker.TV_SIZE.Value, lastParenthetical))
                        continue;
                }

                yield return new Issue(
                    GetTemplate("Warning Tv Position"),
                    null,
                    Capitalize(titleType.type),
                    title
                );
            }
        }

        private IEnumerable<Issue> GetOpVersionIssues(Beatmap beatmap)
        {
            if (!HasGameContext(beatmap.MetadataSettings))
                yield break;

            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                if (string.IsNullOrWhiteSpace(title) || !OpVersionRegex.IsMatch(title))
                    continue;

                if (titleType.type == "unicode" && title == beatmap.MetadataSettings.title)
                    continue;

                if (HasExactMarker(title, Marker.GAME_VER.Value))
                    continue;

                yield return new Issue(
                    GetTemplate("Warning Op Version"),
                    null,
                    Capitalize(titleType.type),
                    title
                );
            }
        }

        private IEnumerable<Issue> GetGuidelineVerIssues(Beatmap beatmap)
        {
            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (titleType.type == "unicode" && title == beatmap.MetadataSettings.title)
                    continue;

                foreach (var issue in GetGenericVersionGuidelineIssues(title, titleType.type))
                    yield return issue;

                foreach (var issue in GetNonCanonicalVerGuidelineIssues(title, titleType.type))
                    yield return issue;

                foreach (var issue in GetLongMarkerGuidelineIssues(title, titleType.type))
                    yield return issue;
            }
        }

        private IEnumerable<Issue> GetGenericVersionGuidelineIssues(string title, string titleType)
        {
            foreach (Match match in GenericVersionMarkerRegex.Matches(title))
            {
                var inner = match.Groups[1].Value.Trim();
                var matchedText = match.Value;

                if (!TryGetGuidelineVerSuggestion(title, inner, matchedText, out var suggested))
                    continue;

                yield return CreateGuidelineVerIssue(titleType, title, suggested);
            }
        }

        private IEnumerable<Issue> GetNonCanonicalVerGuidelineIssues(string title, string titleType)
        {
            foreach (Match match in NonCanonicalParentheticalVerRegex.Matches(title))
            {
                var inner = match.Groups[1].Value.Trim();
                var matchedText = match.Value;

                if (!TryGetGuidelineVerSuggestion(title, inner, matchedText, out var suggested))
                    continue;

                yield return CreateGuidelineVerIssue(titleType, title, suggested);
            }

            foreach (Match match in TrailingUnparenthesizedVerRegex.Matches(title))
            {
                var inner = match.Groups[1].Value.Trim();
                var matchedText = match.Value.Trim();

                if (!TryGetGuidelineVerSuggestion(title, inner, matchedText, out var suggested))
                    continue;

                yield return CreateGuidelineVerIssue(titleType, title, suggested);
            }
        }

        /// <summary> Whether a guideline (#### Ver.) warning should be raised for the matched marker text. </summary>
        private static bool TryGetGuidelineVerSuggestion(
            string title,
            string inner,
            string matchedText,
            out string suggested
        )
        {
            suggested = SuggestGuidelineVerMarker(inner);

            if (IsStylisedMarkerException(inner, matchedText))
                return false;

            if (IsHandledByStandardMarker(inner) || IsHandledByStandardMarker(matchedText))
                return false;

            if (IsMarkerCasingAcceptable(title, suggested, matchedText))
                return false;

            return true;
        }

        private static string SuggestGuidelineVerMarker(string inner)
        {
            if (inner.Equals("extended", StringComparison.OrdinalIgnoreCase))
                return "(Extended Ver.)";

            if (TrySplitCompoundVerInner(inner, out var prefix, out var descriptor))
                return $"({prefix}{ToTitleCaseWords(descriptor)} Ver.)";

            return $"({ToTitleCaseWords(inner)} Ver.)";
        }

        /// <summary>
        /// Parentheticals like ("XETTABYTE" Long Ver.) pair stylised title text with a separate descriptor.
        /// Only the trailing descriptor should be title-cased for the guideline marker.
        /// </summary>
        private static bool TrySplitCompoundVerInner(
            string inner,
            out string prefix,
            out string descriptor
        )
        {
            prefix = "";
            descriptor = inner;

            var lastSpace = inner.LastIndexOf(' ');
            if (lastSpace <= 0)
                return false;

            var beforeDescriptor = inner[..lastSpace];
            if (!ContainsQuotedSegment(beforeDescriptor))
                return false;

            prefix = beforeDescriptor + " ";
            descriptor = inner[(lastSpace + 1)..];
            return true;
        }

        private static bool ContainsQuotedSegment(string text) =>
            text.Contains('"')
            || text.Contains('\u201C')
            || text.Contains('\u201D')
            || text.Contains('\'')
            || text.Contains('\u2018')
            || text.Contains('\u2019');

        private Issue CreateGuidelineVerIssue(string titleType, string title, string suggested) =>
            new(
                GetTemplate("Warning Guideline Ver"),
                null,
                Capitalize(titleType),
                title,
                suggested
            );

        private IEnumerable<Issue> GetLongMarkerGuidelineIssues(string title, string titleType)
        {
            if (!LongMarkerRegex.IsMatch(title))
                yield break;

            const string suggested = "(Long Ver.)";
            if (title.Contains(suggested, StringComparison.OrdinalIgnoreCase))
                yield break;

            yield return CreateGuidelineVerIssue(titleType, title, suggested);
        }

        private IEnumerable<Issue> GetNightcoreIssues(Beatmap beatmap)
        {
            var nightcoreTag = beatmap.MetadataSettings.GetCoveringTag("nightcore");
            if (nightcoreTag == null)
                yield break;

            var spedToNightcore = new SubstitutionPair[]
            {
                new(Marker.SPED_UP_VER, Marker.NIGHTCORE_MIX),
                new(Marker.SPED_UP_CUT_VER, Marker.NIGHTCORE_CUT_VER),
            };

            foreach (var titleType in TitleTypes)
            {
                var title = titleType.Get(beatmap);
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (titleType.type == "unicode" && title == beatmap.MetadataSettings.title)
                    continue;

                foreach (var pair in spedToNightcore)
                {
                    if (!ContainsMarker(title, pair.original.Value))
                        continue;

                    yield return new Issue(
                        GetTemplate("Warning Nightcore Tag"),
                        null,
                        nightcoreTag,
                        pair.substitution.Value,
                        pair.original.Value,
                        titleType.type
                    );
                }
            }
        }

        private static bool HasGameContext(Parser.Settings.MetadataSettings settings) =>
            settings.GetCoveringTag("game") != null || settings.GetCoveringTag("videogame") != null;

        private static bool HasExactMarker(string title, string canonical) =>
            title.Contains(canonical);

        private static bool ContainsMarker(string title, string marker) =>
            title.Contains(marker, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Canonical marker spelling with acceptable casing: exact form, or uniform lower/upper stylisation of the title.
        /// </summary>
        private static bool IsMarkerCasingAcceptable(
            string title,
            string canonical,
            string? markerText = null
        )
        {
            if (!TryResolveMarkerText(title, canonical, markerText, out var slice))
                return false;

            if (slice.Equals(canonical))
                return true;

            if (!slice.Equals(canonical, StringComparison.OrdinalIgnoreCase))
                return false;

            return HasUniformStylisedLetterCasing(title, slice);
        }

        private static bool TryResolveMarkerText(
            string title,
            string canonical,
            string? markerText,
            out string slice
        )
        {
            if (markerText != null)
            {
                slice = markerText;
                return slice.Equals(canonical, StringComparison.OrdinalIgnoreCase);
            }

            return TryGetMarkerOccurrence(title, canonical, out _, out slice);
        }

        private static bool HasUniformStylisedLetterCasing(string title, string markerSlice)
        {
            var index = title.IndexOf(markerSlice, StringComparison.Ordinal);
            if (index < 0)
                index = title.IndexOf(markerSlice, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
                return false;

            var rest = title[..index] + title[(index + markerSlice.Length)..];

            return (IsAllLowercaseLetters(rest) && IsAllLowercaseLetters(markerSlice))
                || (IsAllUppercaseLetters(rest) && IsAllUppercaseLetters(markerSlice));
        }

        /// <summary> Whether an approximate regex match is satisfied by canonical or acceptable stylised casing. </summary>
        private static bool IsApproxMarkerSatisfied(string title, MarkerFormat markerFormat)
        {
            if (!markerFormat.approxRegex.IsMatch(title))
                return false;

            if (HasExactMarker(title, markerFormat.marker.Value))
                return true;

            foreach (Match match in markerFormat.approxRegex.Matches(title))
            {
                var slice = TryExtractParentheticalAt(title, match.Index);
                if (
                    slice != null
                    && IsMarkerCasingAcceptable(title, markerFormat.marker.Value, slice)
                )
                    return true;
            }

            return false;
        }

        private static bool TryGetMarkerOccurrence(
            string title,
            string canonical,
            out int index,
            out string slice
        )
        {
            index = title.IndexOf(canonical, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                slice = "";
                return false;
            }

            slice =
                TryExtractParentheticalAt(title, index)
                ?? title.Substring(index, Math.Min(canonical.Length, title.Length - index));

            return slice.Equals(canonical, StringComparison.OrdinalIgnoreCase);
        }

        private static string? TryExtractParentheticalAt(string title, int indexInMatch)
        {
            var open = title.LastIndexOf('(', indexInMatch);
            if (open < 0)
                return null;

            var close = title.IndexOf(')', open);
            if (close < 0 || close < indexInMatch)
                return null;

            return title.Substring(open, close - open + 1);
        }

        private static bool IsAllLowercaseLetters(string text) =>
            text.Any(char.IsLetter) && text.Where(char.IsLetter).All(char.IsLower);

        private static bool IsAllUppercaseLetters(string text) =>
            text.Any(char.IsLetter) && text.Where(char.IsLetter).All(char.IsUpper);

        private static bool IsWithinLiveMarker(string title, Regex approxRegex)
        {
            var matches = approxRegex.Matches(title);
            if (matches.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                if (!IsInsideLiveParenthetical(title, match.Index))
                    return false;
            }

            return true;
        }

        private static bool IsInsideLiveParenthetical(string title, int matchIndex)
        {
            var open = title.LastIndexOf('(', matchIndex);
            if (open < 0)
                return false;

            var close = title.IndexOf(')', matchIndex);
            if (close < 0)
                return false;

            var segment = title.Substring(open, close - open + 1);
            return LiveMarkerRegex.IsMatch(segment);
        }

        private static string? GetLastParenthetical(string title)
        {
            var close = title.LastIndexOf(')');
            if (close < 0)
                return null;

            var open = title.LastIndexOf('(', close);
            if (open < 0 || open >= close)
                return null;

            return title.Substring(open, close - open + 1);
        }

        /// <summary> Whether the matched text is a stylised marker that should not be standardized. </summary>
        private static bool IsStylisedMarkerException(string inner, string matchedText)
        {
            if (inner.Length > 40)
                return true;

            if (
                Regex.IsMatch(inner, @"(?i)(remix|edition)")
                && !Regex.IsMatch(matchedText, @"^\([^)]+\s+Version\)$", RegexOptions.IgnoreCase)
            )
                return true;

            return false;
        }

        private static bool IsHandledByStandardMarker(string inner)
        {
            foreach (var format in MarkerFormatsOrdered)
            {
                if (format.approxRegex.IsMatch(inner))
                    return true;
            }

            return false;
        }

        private static string ToTitleCaseWords(string text)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(
                ' ',
                words.Select(word =>
                    CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())
                )
            );
        }

        private static string Capitalize(string str) =>
            str.Length == 0 ? str : char.ToUpper(str[0]) + str[1..];

        private sealed class Marker(string value)
        {
            public string Value { get; } = value;

            public static readonly Marker TV_SIZE = new("(TV Size)");
            public static readonly Marker GAME_VER = new("(Game Ver.)");
            public static readonly Marker SHORT_VER = new("(Short Ver.)");
            public static readonly Marker MOVIE_VER = new("(Movie Ver.)");
            public static readonly Marker CUT_VER = new("(Cut Ver.)");
            public static readonly Marker EXTENDED_EDIT = new("(Extended Edit)");
            public static readonly Marker SPED_UP_VER = new("(Sped Up Ver.)");
            public static readonly Marker NIGHTCORE_MIX = new("(Nightcore Mix)");
            public static readonly Marker SPED_UP_CUT_VER = new("(Sped Up & Cut Ver.)");
            public static readonly Marker NIGHTCORE_CUT_VER = new("(Nightcore & Cut Ver.)");
        }

        private readonly struct MarkerFormat(Marker marker, Regex approxRegex)
        {
            public readonly Marker marker = marker;
            public readonly Regex approxRegex = approxRegex;
        }

        private readonly struct TitleType(string type, Func<Beatmap, string> get)
        {
            public readonly string type = type;
            public readonly Func<Beatmap, string> Get = get;
        }

        private readonly struct SubstitutionPair(Marker original, Marker substitution)
        {
            public readonly Marker original = original;
            public readonly Marker substitution = substitution;
        }
    }
}
