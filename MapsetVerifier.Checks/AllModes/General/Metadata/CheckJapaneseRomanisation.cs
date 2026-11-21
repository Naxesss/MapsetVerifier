using System.Globalization;
using System.Text;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckJapaneseRomanisation : GeneralCheck
    {
        private const string Vowels = "aeiou";

        private static readonly Dictionary<char, string[]> KanaRomaji = new()
        {
            ['あ'] = ["a"],
            ['い'] = ["i"],
            ['う'] = ["u"],
            ['え'] = ["e"],
            ['お'] = ["o"],
            ['か'] = ["ka"],
            ['き'] = ["ki"],
            ['く'] = ["ku"],
            ['け'] = ["ke"],
            ['こ'] = ["ko"],
            ['が'] = ["ga"],
            ['ぎ'] = ["gi"],
            ['ぐ'] = ["gu"],
            ['げ'] = ["ge"],
            ['ご'] = ["go"],
            ['さ'] = ["sa"],
            ['し'] = ["shi"],
            ['す'] = ["su"],
            ['せ'] = ["se"],
            ['そ'] = ["so"],
            ['ざ'] = ["za"],
            ['じ'] = ["ji"],
            ['ず'] = ["zu"],
            ['ぜ'] = ["ze"],
            ['ぞ'] = ["zo"],
            ['た'] = ["ta"],
            ['ち'] = ["chi"],
            ['つ'] = ["tsu"],
            ['て'] = ["te"],
            ['と'] = ["to"],
            ['だ'] = ["da"],
            ['ぢ'] = ["ji"],
            ['づ'] = ["zu"],
            ['で'] = ["de"],
            ['ど'] = ["do"],
            ['な'] = ["na"],
            ['に'] = ["ni"],
            ['ぬ'] = ["nu"],
            ['ね'] = ["ne"],
            ['の'] = ["no"],
            // Particles are read differently from how they are written, and we can't tell which use it is.
            ['は'] = ["ha", "wa"],
            ['ひ'] = ["hi"],
            ['ふ'] = ["fu"],
            ['へ'] = ["he", "e"],
            ['ほ'] = ["ho"],
            ['ば'] = ["ba"],
            ['び'] = ["bi"],
            ['ぶ'] = ["bu"],
            ['べ'] = ["be"],
            ['ぼ'] = ["bo"],
            ['ぱ'] = ["pa"],
            ['ぴ'] = ["pi"],
            ['ぷ'] = ["pu"],
            ['ぺ'] = ["pe"],
            ['ぽ'] = ["po"],
            ['ま'] = ["ma"],
            ['み'] = ["mi"],
            ['む'] = ["mu"],
            ['め'] = ["me"],
            ['も'] = ["mo"],
            ['や'] = ["ya"],
            ['ゆ'] = ["yu"],
            ['よ'] = ["yo"],
            ['ら'] = ["ra"],
            ['り'] = ["ri"],
            ['る'] = ["ru"],
            ['れ'] = ["re"],
            ['ろ'] = ["ro"],
            ['わ'] = ["wa"],
            ['ゐ'] = ["i", "wi"],
            ['ゑ'] = ["e", "we"],
            ['を'] = ["o", "wo"],
            ['ん'] = ["n"],
            ['ゔ'] = ["vu"],
            ['ゎ'] = ["wa"],
            ['ゕ'] = ["ka"],
            ['ゖ'] = ["ke"],
        };

        private static readonly Dictionary<char, char> SmallVowels = new()
        {
            ['ぁ'] = 'a',
            ['ぃ'] = 'i',
            ['ぅ'] = 'u',
            ['ぇ'] = 'e',
            ['ぉ'] = 'o',
        };

        private static readonly Dictionary<char, char> SmallY = new()
        {
            ['ゃ'] = 'a',
            ['ゅ'] = 'u',
            ['ょ'] = 'o',
        };

        private const char SmallTsu = 'っ';
        private const char LongVowelMark = 'ー';

        // Vowel pairs that Modified Hepburn writes as a single long vowel (macron), which an ASCII field
        // can spell either way. "ei" and "ii" are kept as written.
        private static readonly HashSet<string> LongVowelPairs = ["aa", "uu", "ee", "oo", "ou"];

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Japanese romanisation.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out romanised artists and titles that don't follow the Modified Hepburn system when the
                        unicode field is written in hiragana, and romanised artist names whose name order is swapped."
                    },
                    {
                        "Reasoning",
                        @"
                        Japanese metadata must be romanised with the Modified Hepburn system, and artist names must keep
                        the order they are printed in the unicode field. Consistent romanisation keeps the same song or
                        artist searchable and recognisable across beatmaps.

                        Official romanisations are used as-is instead, which is why this is a warning rather than a problem."
                    },
                    {
                        "Limitations",
                        @"
                        Only fields written in hiragana, optionally mixed with plain latin text, are checked. Kanji can't be
                        read without a dictionary, and katakana is mostly loan words, which use the source language's
                        spelling instead.

                        Long vowels are accepted with or without the repeated vowel (e.g. *ou* or *o*), and particles are
                        accepted as either reading (e.g. *ha* or *wa*). Word spacing and capitalisation aren't checked."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Hepburn",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Romanised {0} \"{1}\" doesn't match \"{2}\" in Modified Hepburn, expected something like \"{3}\". Ignore if this is an official romanisation.",
                        "artist/title",
                        "romanised",
                        "unicode",
                        "suggestion"
                    )
                        .WithCause(
                            "The unicode artist or title is written in hiragana, and the romanised field spells it differently from Modified Hepburn."
                        )
                        .WithRule(RC.Metadata.Romanisation_JapaneseUseModifiedHepburnSystem)
                },
                {
                    "Name Order",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "Romanised artist \"{0}\" swaps the name order of \"{1}\", expected something like \"{2}\". Ignore if this is an official romanisation.",
                        "romanised",
                        "unicode",
                        "suggestion"
                    )
                        .WithCause(
                            "The unicode artist is written in hiragana, and the romanised artist lists its names in the opposite order."
                        )
                        .WithRule(RC.Metadata.Romanisation_ArtistNamesRomanisedOrderPrinted)
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var pairs = beatmapSet
                .Beatmaps.SelectMany(beatmap =>
                    new[]
                    {
                        (
                            "artist",
                            beatmap.MetadataSettings.artist,
                            beatmap.MetadataSettings.artistUnicode
                        ),
                        (
                            "title",
                            beatmap.MetadataSettings.title,
                            beatmap.MetadataSettings.titleUnicode
                        ),
                    }
                )
                .Distinct();

            foreach (var (field, romanised, unicode) in pairs)
            {
                var issue = GetIssue(field, romanised, unicode);
                if (issue != null)
                    yield return issue;
            }
        }

        private Issue? GetIssue(string field, string? romanised, string? unicode)
        {
            if (string.IsNullOrWhiteSpace(romanised) || string.IsNullOrWhiteSpace(unicode))
                return null;

            // Non-ASCII in the romanised field is reported by the unicode check instead.
            if (romanised.Any(ch => ch > 127))
                return null;

            var units = Parse(unicode);
            if (units == null)
                return null;

            var target = Letters(romanised);
            if (Matches(units, target))
                return null;

            if (field == "artist")
            {
                var reversedUnicode = string.Join(
                    " ",
                    unicode
                        .Normalize(NormalizationForm.FormKC)
                        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                        .Reverse()
                );
                var reversedUnits = Parse(reversedUnicode);
                if (
                    reversedUnicode != unicode.Normalize(NormalizationForm.FormKC).Trim()
                    && reversedUnits != null
                    && Matches(reversedUnits, target)
                )
                    return new Issue(
                        GetTemplate("Name Order"),
                        null,
                        romanised,
                        unicode,
                        Suggest(unicode)
                    );
            }

            return new Issue(
                GetTemplate("Hepburn"),
                null,
                field,
                romanised,
                unicode,
                Suggest(unicode)
            );
        }

        private sealed class Unit(char? kana, List<string> alternatives)
        {
            public char? Kana { get; } = kana;
            public List<string> Alternatives { get; set; } = alternatives;
        }

        /// <summary>
        ///     Splits the unicode field into units, each with every accepted romanisation. Separators like spaces
        ///     and punctuation are left out, since only letters are compared. Returns null when the field isn't
        ///     something this check can read: no hiragana, or any kanji, katakana or other script.
        /// </summary>
        private static List<Unit>? Parse(string unicode)
        {
            var text = unicode.Normalize(NormalizationForm.FormKC);
            var units = new List<Unit>();
            var hasHiragana = false;
            // Whether the previous unit sits directly before this character, with no separator in between.
            var adjacent = false;
            var pendingTsu = false;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                var previous = adjacent && units.Count > 0 ? units[^1] : null;

                if (ch < 128)
                {
                    if (char.IsLetter(ch))
                    {
                        AddUnit(new Unit(null, [char.ToLowerInvariant(ch).ToString()]));
                        continue;
                    }

                    FlushTsu();
                    adjacent = false;
                    continue;
                }

                if (KanaRomaji.TryGetValue(ch, out var romaji))
                {
                    hasHiragana = true;
                    var unit = new Unit(ch, [.. romaji]);
                    if (previous?.Kana != null && "あいうえお".Contains(ch))
                        AddLongVowelAlternatives(previous, unit, romaji[0][0]);
                    AddUnit(unit);
                    continue;
                }

                if (SmallY.TryGetValue(ch, out var yVowel))
                {
                    hasHiragana = true;
                    if (
                        previous?.Kana != null
                        && previous.Alternatives.All(alt => alt.EndsWith('i'))
                    )
                        previous.Alternatives = previous
                            .Alternatives.Select(alt =>
                                alt.EndsWith("shi") || alt.EndsWith("chi") || alt.EndsWith("ji")
                                    ? alt[..^1] + yVowel
                                    : alt[..^1] + "y" + yVowel
                            )
                            .ToList();
                    else
                        AddUnit(new Unit(ch, ["y" + yVowel]));
                    continue;
                }

                if (SmallVowels.TryGetValue(ch, out var vowel))
                {
                    hasHiragana = true;
                    if (previous?.Kana != null && previous.Alternatives.All(alt => alt.Length > 0))
                        previous.Alternatives = previous
                            .Alternatives.SelectMany(alt =>
                                new[]
                                {
                                    previous.Kana switch
                                    {
                                        'う' => alt[..^1] + "w" + vowel,
                                        'ゔ' => alt[..^2] + "v" + vowel,
                                        _ => alt[..^1] + vowel,
                                    },
                                    alt + vowel,
                                }
                            )
                            .Distinct()
                            .ToList();
                    else
                        AddUnit(new Unit(ch, [vowel.ToString()]));
                    continue;
                }

                if (ch == SmallTsu)
                {
                    hasHiragana = true;
                    FlushTsu();
                    pendingTsu = true;
                    adjacent = true;
                    continue;
                }

                if (
                    ch == LongVowelMark
                    && previous?.Kana != null
                    && previous.Alternatives[0].Length > 0
                )
                {
                    var last = previous.Alternatives[0][^1];
                    var alternatives = new List<string> { "" };
                    if (Vowels.Contains(last))
                        alternatives.Insert(0, last.ToString());
                    AddUnit(new Unit(ch, alternatives));
                    continue;
                }

                // Kanji, katakana, iteration marks and other scripts can't be romanised here.
                if (char.IsLetter(ch) || IsKatakana(ch) || ch is 'ゝ' or 'ゞ')
                    return null;

                FlushTsu();
                adjacent = false;
            }

            FlushTsu();
            return hasHiragana ? units : null;

            void AddUnit(Unit unit)
            {
                if (pendingTsu)
                {
                    unit.Alternatives = unit.Alternatives.Select(Geminate).ToList();
                    pendingTsu = false;
                }

                units.Add(unit);
                adjacent = true;
            }

            // A small tsu that isn't followed by a kana it can double, like at the end of a word.
            void FlushTsu()
            {
                if (!pendingTsu)
                    return;

                pendingTsu = false;
                units.Add(new Unit(SmallTsu, ["", "t", "tsu"]));
            }
        }

        private static string Geminate(string romaji)
        {
            if (romaji.StartsWith("ch"))
                return "t" + romaji;
            if (romaji.Length > 0 && !Vowels.Contains(romaji[0]))
                return romaji[0] + romaji;
            return romaji;
        }

        private static void AddLongVowelAlternatives(Unit previous, Unit unit, char vowel)
        {
            var lastVowels = previous
                .Alternatives.Select(alt => alt.Length > 0 ? alt[^1] : ' ')
                .Distinct()
                .ToList();
            if (lastVowels.Count != 1 || !LongVowelPairs.Contains($"{lastVowels[0]}{vowel}"))
                return;

            unit.Alternatives.Add("");
            if (lastVowels[0] == 'o')
                unit.Alternatives.Add("h");
        }

        private static bool IsKatakana(char ch) => ch is >= 'ァ' and <= 'ヺ' or 'ヽ' or 'ヾ';

        private static string Letters(string text) =>
            new(text.ToLowerInvariant().Where(ch => ch is >= 'a' and <= 'z').ToArray());

        /// <summary> Whether the letters of the romanised field can be built from the units in order. </summary>
        private static bool Matches(List<Unit> units, string target)
        {
            var positions = new HashSet<int> { 0 };
            foreach (var unit in units)
            {
                var next = new HashSet<int>();
                foreach (var position in positions)
                foreach (var alternative in unit.Alternatives)
                    if (target.AsSpan(position).StartsWith(alternative))
                        next.Add(position + alternative.Length);

                if (next.Count == 0)
                    return false;
                positions = next;
            }

            return positions.Contains(target.Length);
        }

        /// <summary> The first accepted romanisation of each word, with spacing kept and words capitalised. </summary>
        private static string Suggest(string unicode)
        {
            var words = unicode
                .Normalize(NormalizationForm.FormKC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                {
                    var units = Parse(word);
                    if (units == null)
                        return new string(word.Where(ch => ch < 128).ToArray());

                    var romaji = string.Concat(units.Select(unit => unit.Alternatives[0]));
                    return romaji.Length == 0
                        ? romaji
                        : char.ToUpper(romaji[0], CultureInfo.InvariantCulture) + romaji[1..];
                })
                .Where(word => word.Length > 0);

            return string.Join(" ", words);
        }
    }
}
