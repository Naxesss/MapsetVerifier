using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.Checks.Utils.Romanisation;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public partial class CheckRomanisation : GeneralCheck
    {
        // Each language the Ranking Criteria gives romanisation rules for gets a romaniser here.
        // Romanisers that need a language tag come before the ones that don't, so the tag decides when both apply.
        private static readonly Romaniser[] Romanisers =
        [
            new ChineseRomaniser(),
            new JapaneseRomaniser(),
            new CyrillicRomaniser(),
            LatinRomaniser.German,
            LatinRomaniser.Swedish,
            LatinRomaniser.Finnish,
            LatinRomaniser.Nordic,
            LatinRomaniser.AnyLanguage,
        ];

        private const string NameOrderTemplate = "Name Order";
        private const string MissingTemplate = "Missing";
        private const string RepeatTemplate = "Repeat";

        /// <summary> How many names of an artist field are looked at being the other way around. </summary>
        private const int MaxNameSwaps = 4;

        private const string MisspelledTemplate = "Misspelled";

        /// <summary> Fields with fewer letters than this may well be other words if they're a letter off. </summary>
        private const int MinLettersToCorrect = 4;

        private const string UnconfirmedTemplate = "Unconfirmed";

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Romanisation.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out romanised artists and titles that don't follow the romanisation system the Ranking
                        Criteria asks for, romanised artist names whose name order is swapped, romanised fields that
                        leave out part of the unicode field entirely, and romanised fields that write a word twice
                        because the unicode field has both the word and its romanisation."
                    },
                    {
                        "Reasoning",
                        @"
                        Metadata in other scripts must be romanised with a set system per language, and artist names must
                        keep the order they are printed in the unicode field. Consistent romanisation keeps the same song
                        or artist searchable and recognisable across beatmaps.

                        Official romanisations are used as-is instead, which is why these are warnings rather than problems."
                    },
                    {
                        "Covered languages",
                        @"
                        - **Japanese**, Modified Hepburn: fields written in kana and kanji, optionally mixed with plain latin
                          text. Kanji are read with built-in dictionaries, which also know the source spelling of common
                          loan words and names (*ハート* is *Heart*). Only a romanisation in another system like Kunrei-shiki is
                          clearly wrong (e.g. *tyo* instead of *cho*, *tu* instead of *tsu*). Any other difference from the
                          readings and spellings they know is pointed out as well, so that mistakes aren't missed, but those
                          are often correct, such as with translations and stage names. If it's only a letter off, or has two
                          letters swapped, the corrected field is suggested.
                        - **Chinese**, for beatmapsets tagged as Chinese: each character must be its own capitalised word
                          (*Wo Ai Ni*), so a romanised field whose letters follow the pinyin but run characters together is
                          reported. Proper nouns are exempt, so ignore it for those. A romanised field that doesn't follow
                          the pinyin at all, like a translation or Cantonese, isn't reported.
                        - **Cyrillic**, BGN/PCGN: reported when the romanised field clearly follows another system, like
                          *Jurij* instead of *Yuriy*. Russian, Ukrainian and Belarusian readings of shared letters are all
                          accepted.
                        - **German, Swedish, Finnish and Nordic** letters (*ü* as *ue*, *ö* as *o*, *å* as *aa*): the rules of
                          the language the beatmapset is tagged with. Without one of those tags, any of their spellings is
                          accepted, and only ones none of them allow are reported, like *ø* as *o*. Other accented letters
                          are expected as their plain letter (*é* as *e*)."
                    },
                    {
                        "Limitations",
                        @"
                        Only the letters are compared, so spacing, punctuation and capitalisation aren't checked. Readings
                        that can't be told apart are all accepted, such as long vowels with or without the repeated vowel
                        (*ou* or *o*) and particles (*ha* or *wa*).

                        Every reading the dictionaries know for a word or name is accepted, so a wrong one that exists isn't
                        pointed out. Creative readings (*未来* sung as *ashita*) and official spellings they don't know are
                        pointed out even if correct. Words that can't be read at all leave the field unchecked, as do fields
                        in kanji alone without a Japanese tag when they don't match.

                        Name order is checked by putting romanised artist names the other way around.

                        A left-out part is found by lining up the latin text both fields share, such as *feat.* or
                        *(TV Size)*. A part that repeats the latin text next to it may be left out, so a part is only
                        reported when it can be read and the text beside it isn't its romanisation.

                        A repeated word is only looked for when the unicode field sets it apart with brackets, quotes,
                        dashes, tildes or a slash (*ソレイユ -Soleil-*). The word and its romanisation must both read the
                        same, or be spelled the same in the romanised field. A word the unicode field has more than once
                        in its own script is a genuine repeat."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            var templates = Romanisers.ToDictionary(
                romaniser => romaniser.Name,
                romaniser =>
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} \"{1}\" should be \"{2}\", unless official.",
                        "Artist/Title",
                        "romanised",
                        "suggestion"
                    )
                        .WithCause(romaniser.Cause)
                        .WithRule(romaniser.Rules)
            );

            foreach (
                var romaniser in Romanisers.Where(romaniser => romaniser.FormatMessage != null)
            )
                templates[FormatTemplate(romaniser)] = new IssueTemplate(
                    Issue.Level.Warning,
                    romaniser.FormatMessage!,
                    "Artist/Title",
                    "romanised",
                    "suggestion"
                )
                    .WithCause(romaniser.Cause)
                    .WithRule(romaniser.Rules);

            templates[NameOrderTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "{0} \"{1}\" should be \"{2}\", unless official.",
                "Artist/Title",
                "romanised",
                "suggestion"
            )
                .WithCause(
                    "The romanised artist matches the unicode artist only when its names are put in the opposite order."
                )
                .WithRule(RC.Metadata.Romanisation_ArtistNamesRomanisedOrderPrinted);

            templates[MissingTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "{0} \"{1}\" leaves out \"{2}\".",
                "Artist/Title",
                "romanised",
                "missing part"
            ).WithCause(
                "Part of the unicode artist or title is written in a non-latin script, and the romanised field has nothing in its place while the latin text around it matches."
            );

            templates[RepeatTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "{0} \"{1}\" has \"{2}\" twice.",
                "Artist/Title",
                "romanised",
                "repeated part"
            )
                .WithCause(
                    "The unicode artist or title has both a word and its romanisation, and the romanised field has the romanisation of both."
                )
                .WithRule(RC.Metadata.Romanisation_SongUsesRepeatWordsTitle);

            templates[MisspelledTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "{0} \"{1}\" should be \"{2}\", unless official.",
                "Artist/Title",
                "romanised",
                "correction"
            )
                .WithCause(
                    "The unicode artist or title is written in Japanese, and the romanised field is a single letter off, or has two letters swapped, compared to how it's read."
                )
                .WithRule(RC.Metadata.Romanisation_JapaneseUseModifiedHepburnSystem);

            templates[UnconfirmedTemplate] = new IssueTemplate(
                Issue.Level.Warning,
                "{0} \"{1}\" doesn't match \"{2}\", ensure it's official.",
                "Artist/Title",
                "romanised",
                "suggestion"
            )
                .WithCause(
                    "The unicode artist or title is written in Japanese, and the romanised field neither matches how it's read nor clearly follows another system, such as with loan words, translations and names."
                )
                .WithRule(RC.Metadata.Romanisation_JapaneseUseModifiedHepburnSystem);

            return templates;
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var fields = beatmapSet
                .Beatmaps.SelectMany(beatmap =>
                    new[]
                    {
                        (
                            "Artist",
                            beatmap.MetadataSettings.artist,
                            beatmap.MetadataSettings.artistUnicode
                        ),
                        (
                            "Title",
                            beatmap.MetadataSettings.title,
                            beatmap.MetadataSettings.titleUnicode
                        ),
                    }
                )
                .Distinct();

            var tags = beatmapSet
                .Beatmaps.SelectMany(beatmap =>
                    beatmap.MetadataSettings.tags.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                )
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (field, romanised, unicode) in fields)
            {
                var issue = GetIssue(field, romanised, unicode, tags);
                if (issue != null)
                    yield return issue;
            }
        }

        private Issue? GetIssue(
            string field,
            string? romanised,
            string? unicode,
            IReadOnlyCollection<string> tags
        )
        {
            if (string.IsNullOrWhiteSpace(romanised) || string.IsNullOrWhiteSpace(unicode))
                return null;

            // Non-ASCII in the romanised field is reported by the unicode check instead.
            if (romanised.Any(ch => ch > 127))
                return null;

            var repeat = FindRepeatedPart(romanised, unicode, tags);
            if (repeat != null)
                return new Issue(GetTemplate(RepeatTemplate), null, field, romanised, repeat);

            var missing = FindMissingPart(romanised, unicode, tags);
            if (missing != null)
                return new Issue(GetTemplate(MissingTemplate), null, field, romanised, missing);

            var romaniser = Romanisers.FirstOrDefault(romaniser =>
                romaniser.AppliesTo(unicode, tags)
            );
            if (romaniser == null)
                return null;

            var verdict = romaniser.Judge(unicode, romanised);
            if (verdict == RomanisationVerdict.Matches)
                return
                    romaniser.FormatMessage == null || romaniser.FollowsFormat(unicode, romanised)
                    ? null
                    : new Issue(
                        GetTemplate(FormatTemplate(romaniser)),
                        null,
                        field,
                        romanised,
                        romaniser.Suggest(unicode)
                    );

            if (verdict == RomanisationVerdict.Unreadable)
                return null;

            if (
                field == "Artist"
                && FindPrintedOrder(romaniser, unicode, romanised) is { } printedOrder
            )
                return new Issue(
                    GetTemplate(NameOrderTemplate),
                    null,
                    field,
                    romanised,
                    printedOrder
                );

            if (verdict == RomanisationVerdict.Inconclusive)
            {
                if (
                    !romaniser.PointsOutInconclusive(unicode, tags)
                    || KeepsRepeatOnce(romaniser, unicode, romanised)
                )
                    return null;

                if (
                    romaniser.Correct(unicode, romanised) is { } correction
                    && IsTypo(correction, romanised)
                )
                    return new Issue(
                        GetTemplate(MisspelledTemplate),
                        null,
                        field,
                        romanised,
                        correction.Text
                    );

                return new Issue(
                    GetTemplate(UnconfirmedTemplate),
                    null,
                    field,
                    romanised,
                    romaniser.Suggest(unicode)
                );
            }

            return new Issue(
                GetTemplate(romaniser.Name),
                null,
                field,
                romanised,
                romaniser.Suggest(unicode)
            );
        }

        /// <summary>
        ///     Finds a run of non-latin script in the unicode field that has nothing in its place in the romanised
        ///     field, using the latin text around it as anchors. Returns null when nothing is missing, or when the
        ///     latin text itself differs, since the fields can't be lined up then.
        ///
        ///     A word may be left out when it repeats latin text next to it, like "夜に駆ける (Yoru ni Kakeru)"
        ///     romanised as "Yoru ni Kakeru". So a missing run is only reported when a romaniser can read it and the
        ///     latin text beside it isn't its romanisation. Runs no romaniser can read aren't reported.
        /// </summary>
        private static string? FindMissingPart(
            string romanised,
            string unicode,
            IReadOnlyCollection<string> tags
        )
        {
            var segments = new List<(bool IsScript, StringBuilder Text)>();
            foreach (var ch in unicode.Normalize(NormalizationForm.FormKC))
            {
                var isLatin = ch < 128 && char.IsLetterOrDigit(ch);
                var isScript = ch >= 128 && char.IsLetter(ch);
                if (isLatin || isScript)
                {
                    if (segments.Count == 0 || segments[^1].IsScript != isScript)
                        segments.Add((isScript, new StringBuilder()));
                    segments[^1].Text.Append(isLatin ? char.ToLowerInvariant(ch) : ch);
                }
                else if (segments.Count > 0 && segments[^1].IsScript)
                    // Keep spacing inside a script run, so the missing part reads as written.
                    segments[^1].Text.Append(ch);
            }

            if (!segments.Any(segment => segment.IsScript))
                return null;

            var target = new string(
                romanised
                    .ToLowerInvariant()
                    .Where(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9')
                    .ToArray()
            );

            var lenient = Regex.Match(target, BuildPattern(segments, "(.*)"));
            if (!lenient.Success || Regex.IsMatch(target, BuildPattern(segments, "(.+)")))
                return null;

            var group = 1;
            for (var i = 0; i < segments.Count; i++)
            {
                if (!segments[i].IsScript || lenient.Groups[group++].Length > 0)
                    continue;

                // Separators after the run were kept for spacing inside it; drop those around its ends.
                var text = segments[i].Text.ToString();
                var start = 0;
                var end = text.Length;
                while (start < end && !char.IsLetter(text[start]))
                    start++;
                while (end > start && !char.IsLetter(text[end - 1]))
                    end--;
                var run = text[start..end];
                var romaniser = Romanisers.FirstOrDefault(romaniser =>
                    romaniser.AppliesTo(run, tags)
                );
                if (romaniser == null)
                    continue;

                var neighbours = new[] { i - 1, i + 1 }
                    .Where(index => index >= 0 && index < segments.Count)
                    .Select(index => segments[index].Text.ToString());
                var repeatsNeighbour = neighbours.Any(neighbour =>
                    romaniser.Judge(run, neighbour)
                        is not (RomanisationVerdict.Inconclusive or RomanisationVerdict.Unreadable)
                );
                if (
                    romaniser.Judge(run, "") != RomanisationVerdict.Unreadable
                    && !repeatsNeighbour
                    && !MayBeRepeatedLoanWord(run, unicode)
                )
                    return run;
            }

            return null;
        }

        /// <summary>
        ///     Finds a part that's twice in the romanised field, because the unicode field has a word in its own script
        ///     next to its romanisation, like "夜に駆ける (Yoru ni Kakeru)" romanised as
        ///     "Yoru ni Kakeru (Yoru ni Kakeru)". Returns null when nothing is repeated.
        /// </summary>
        private static string? FindRepeatedPart(
            string romanised,
            string unicode,
            IReadOnlyCollection<string> tags
        )
        {
            var text = unicode.Normalize(NormalizationForm.FormKC);
            var words = LatinWord().Matches(romanised).ToList();
            var unicodeWords = LatinWord().Matches(text).ToList();

            foreach (Match match in SetApartPart().Matches(text))
            {
                var inside = match.Groups["inside"].Value.Trim();

                // Either side may be the romanisation, "夜 (Yoru)" or "Yoru (夜)", so the part set apart must be one
                // or the other as a whole.
                var latinIsInside = !inside.Any(IsScriptLetter);
                if (!latinIsInside && inside.Any(char.IsAsciiLetterOrDigit))
                    continue;

                // The part set apart belongs to the words right before it, however many those are.
                var outside = text[..match.Index].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (var start = 0; start < outside.Length; start++)
                {
                    var neighbour = string.Join(' ', outside[start..]);
                    var (run, latin) = latinIsInside ? (neighbour, inside) : (inside, neighbour);
                    if (!run.Any(IsScriptLetter) || latin.Any(IsScriptLetter))
                        continue;

                    // A word that's there more than once in its own script is meant to be repeated (つき つき, バイバイ).
                    if (Regex.Count(text, Regex.Escape(run)) > 1 || Doubled().IsMatch(run))
                        continue;

                    var latinLetters = LettersAndDigits(latin);
                    if (latinLetters.Count(char.IsAsciiLetter) < 2)
                        continue;

                    var isRomanisation = IsRomanisation(run, latin, tags);
                    var wordsAfter = LatinWord().Count(text[(match.Index + match.Length)..]);
                    foreach (
                        var (pair, first, end) in FindNextTo(words, latinLetters, latinIsInside)
                    )
                    {
                        var readsTheSame = isRomanisation && IsRomanisation(run, pair, tags);

                        // Loan words can't be told to be a romanisation, but being spelled the same gives them away.
                        // The two must be all there is besides what follows the part set apart, since other text
                        // could have the same word too.
                        var isSpelledTheSame =
                            LettersAndDigits(pair) == latinLetters
                            && start == 0
                            && !run.Any(char.IsAsciiLetterOrDigit)
                            && first == 0
                            && end == words.Count - wordsAfter;

                        // Latin text the unicode field itself repeats stays repeated.
                        var pairLetters = LettersAndDigits(pair);
                        if (
                            (readsTheSame || isSpelledTheSame)
                            && CountWords(words, pairLetters)
                                > CountWords(unicodeWords, pairLetters)
                        )
                            return pair;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Every group of words directly before or after the latin text in the romanised field, with the indexes
        ///     where the two of them together start and end.
        /// </summary>
        private static IEnumerable<(string Pair, int First, int End)> FindNextTo(
            List<Match> words,
            string latinLetters,
            bool before
        )
        {
            for (var start = 0; start < words.Count; start++)
            for (var end = start + 1; end <= words.Count; end++)
            {
                if (JoinWords(words, start, end) != latinLetters)
                    continue;

                if (before)
                    for (var first = 0; first < start; first++)
                        yield return (Spaced(first, start), first, end);
                else
                    for (var last = end + 1; last <= words.Count; last++)
                        yield return (Spaced(end, last), start, last);
            }

            string Spaced(int from, int to) =>
                string.Join(' ', words[from..to].Select(word => word.Value));
        }

        /// <summary> How often the letters can be put together from whole words. </summary>
        private static int CountWords(List<Match> words, string letters)
        {
            var count = 0;
            for (var start = 0; start < words.Count; start++)
            for (var end = start + 1; end <= words.Count; end++)
                if (JoinWords(words, start, end) == letters)
                    count++;

            return count;
        }

        private static string JoinWords(List<Match> words, int start, int end) =>
            LettersAndDigits(string.Concat(words[start..end].Select(word => word.Value)));

        private static bool IsRomanisation(
            string run,
            string latin,
            IReadOnlyCollection<string> tags
        ) =>
            Romanisers.Any(romaniser =>
                romaniser.AppliesTo(run, tags)
                && romaniser.Judge(run, latin) == RomanisationVerdict.Matches
            );

        private static bool IsScriptLetter(char ch) => ch >= 128 && char.IsLetter(ch);

        /// <summary>
        ///     Whether the romanised field is only one side of a unicode field that has both a word and its romanisation
        ///     or translation, like "夜に駆ける (Yoru ni Kakeru)" romanised as "Yoru ni Kakeru".
        /// </summary>
        private static bool KeepsRepeatOnce(Romaniser romaniser, string unicode, string romanised)
        {
            var text = unicode.Normalize(NormalizationForm.FormKC);

            return SetApartPart()
                .Matches(text)
                .SelectMany(match =>
                    new[]
                    {
                        text.Remove(match.Index, match.Length),
                        text.Remove(match.Index, match.Length)[match.Index..]
                            .Insert(0, match.Groups["inside"].Value),
                    }
                )
                .Any(side =>
                    side.Any(IsScriptLetter)
                        ? romaniser.Judge(side, romanised) == RomanisationVerdict.Matches
                        : LettersAndDigits(side) == LettersAndDigits(romanised)
                );
        }

        /// <summary>
        ///     Whether only a single letter was changed, or two were swapped. Anything more is likely about different
        ///     words, for which the closest reading would be a wrong suggestion.
        /// </summary>
        private static bool IsTypo(RomanisationCorrection correction, string romanised) =>
            correction.Edits == 1 && romanised.Count(char.IsAsciiLetter) >= MinLettersToCorrect;

        private static string LettersAndDigits(string text) =>
            new(text.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

        /// <summary>
        ///     Whether the run is a katakana word set apart from latin text, like "ソレイユ -Soleil-", which may be left
        ///     out when it's the same word. Loan words can't be read, so this can't be told apart from different words.
        /// </summary>
        private static bool MayBeRepeatedLoanWord(string run, string unicode)
        {
            if (!run.All(ch => ch is >= 'ァ' and <= 'ー' || !char.IsLetter(ch)))
                return false;

            var text = unicode.Normalize(NormalizationForm.FormKC);
            return SetApartPart()
                .Matches(text)
                .Any(match =>
                {
                    var before = text[..match.Index].TrimEnd();
                    var inside = match.Groups["inside"].Value.Trim();
                    return (inside == run && before.Any(char.IsAsciiLetter))
                        || (
                            before.EndsWith(run)
                            && inside.Any(char.IsAsciiLetter)
                            && !Marker().IsMatch(inside)
                        );
                });
        }

        [GeneratedRegex("[A-Za-z0-9']+")]
        private static partial Regex LatinWord();

        [GeneratedRegex(@"^(.+?)(?:\s*\1)+$")]
        private static partial Regex Doubled();

        [GeneratedRegex(
            @"\b(?:size|ver|version|cut|edit|feat|ft|cv|cover|mix|remix|rmx)\b",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex Marker();

        // "(Soleil)", "-Soleil-" and "/ Soleil". A dash or tilde right after a latin letter joins words instead.
        [GeneratedRegex(
            @"[(\[{<「『【《〈“""](?<inside>[^()\[\]{}<>「」『』【】《》〈〉“”""]+)[)\]}>」』】》〉”""]"
                + @"|(?<![A-Za-z0-9])(?<dash>[-~〜])(?<inside>(?:(?!\k<dash>).)+)\k<dash>(?=\s|$)"
                + @"|\s*[/|]\s*(?<inside>[^/|]+)$"
        )]
        private static partial Regex SetApartPart();

        private static string BuildPattern(
            List<(bool IsScript, StringBuilder Text)> segments,
            string scriptPattern
        ) =>
            "^"
            + string.Concat(
                segments.Select(segment =>
                    segment.IsScript ? scriptPattern : Regex.Escape(segment.Text.ToString())
                )
            )
            + "$";

        private static string FormatTemplate(Romaniser romaniser) => $"{romaniser.Name} Format";

        /// <summary>
        ///     The romanised field with as few names as possible put the other way around so that it matches the unicode
        ///     field, or null if no such order matches.
        /// </summary>
        private static string? FindPrintedOrder(
            Romaniser romaniser,
            string unicode,
            string romanised
        )
        {
            var pairs = NamePair().Matches(romanised).ToList();

            for (var swaps = 1; swaps <= MaxNameSwaps; swaps++)
                foreach (var chosen in Choose(pairs, 0, swaps))
                {
                    var swapped = romanised;
                    // Swapping a pair keeps its length, so the other pairs stay where they are.
                    foreach (var pair in chosen)
                        swapped =
                            swapped[..pair.Index]
                            + $"{pair.Groups["second"].Value} {pair.Groups["first"].Value}"
                            + swapped[(pair.Index + PairLength(pair))..];

                    if (romaniser.Judge(unicode, swapped) == RomanisationVerdict.Matches)
                        return swapped;
                }

            return null;
        }

        /// <summary> Every way to pick a number of pairs which don't share a word. </summary>
        private static IEnumerable<List<Match>> Choose(List<Match> pairs, int from, int count)
        {
            if (count == 0)
            {
                yield return [];
                yield break;
            }

            for (var i = from; i < pairs.Count; i++)
            {
                var next = i + 1;
                while (
                    next < pairs.Count && pairs[next].Index < pairs[i].Index + PairLength(pairs[i])
                )
                    next++;

                foreach (var rest in Choose(pairs, next, count - 1))
                    yield return [pairs[i], .. rest];
            }
        }

        private static int PairLength(Match pair) =>
            pair.Groups["first"].Length + 1 + pair.Groups["second"].Length;

        // Found at every word, so that pairs can overlap.
        [GeneratedRegex(@"(?<![A-Za-z'])(?=(?<first>[A-Za-z']+) (?<second>[A-Za-z']+))")]
        private static partial Regex NamePair();
    }
}
