using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Japanese kana in Modified Hepburn. Kanji can't be read without a dictionary, so fields containing kanji
///     are left unread.
/// </summary>
public sealed class JapaneseRomaniser : Romaniser
{
    private const string Vowels = "aeiou";
    private const char SmallTsu = 'っ';
    private const char LongVowelMark = 'ー';

    // Keyed by hiragana; katakana is folded onto hiragana before lookup.
    private static readonly Dictionary<char, string[]> Hepburn = new()
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
        // Katakana only, no hiragana equivalent.
        ['ヷ'] = ["va"],
        ['ヸ'] = ["vi"],
        ['ヹ'] = ["ve"],
        ['ヺ'] = ["vo"],
    };

    // Spellings from Kunrei-shiki, Nihon-shiki and traditional Hepburn, which aren't Modified Hepburn.
    private static readonly Dictionary<char, string[]> OtherSystems = new()
    {
        ['し'] = ["si"],
        ['ち'] = ["ti"],
        ['つ'] = ["tu"],
        ['ふ'] = ["hu"],
        ['じ'] = ["zi"],
        ['ぢ'] = ["di", "zi"],
        ['づ'] = ["du"],
        ['ん'] = ["m"],
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

    // Vowel pairs that Modified Hepburn writes as a single long vowel (macron), which an ASCII field
    // can spell either way. "ei" and "ii" are kept as written.
    private static readonly HashSet<string> LongVowelPairs = ["aa", "uu", "ee", "oo", "ou"];

    public override string Name => "Japanese";
    public override string SystemName => "Modified Hepburn";
    public override string Rule => RC.Metadata.Romanisation_JapaneseUseModifiedHepburnSystem;

    public override string Cause =>
        "The unicode artist or title is written in kana, and the romanised field follows another system like Kunrei-shiki instead of Modified Hepburn.";

    protected override bool ContainsScript(string text) => text.Any(IsKana);

    protected override List<RomanisationUnit>? Read(string text) => Read(text, anySystem: false);

    protected override List<RomanisationUnit>? ReadAnySystem(string text) =>
        Read(text, anySystem: true);

    // Kana is often used for loan words and official names (かめりあ → Camellia, えくすぷれす → Express), which
    // keep their own spelling. Only spellings that clearly come from another system are reported.
    protected override bool MayUseDifferentWords(string text) => true;

    private static List<RomanisationUnit>? Read(string text, bool anySystem)
    {
        var units = new List<RomanisationUnit>();
        // Whether the previous unit sits directly before this character, with no separator in between.
        var adjacent = false;
        var pendingTsu = false;

        foreach (var original in text)
        {
            var ch = ToHiragana(original);
            var previous = adjacent && units.Count > 0 ? units[^1] : null;

            if (ch < 128)
            {
                if (char.IsLetter(ch))
                {
                    AddUnit(new RomanisationUnit(null, [char.ToLowerInvariant(ch).ToString()]));
                    continue;
                }

                FlushTsu();
                adjacent = false;
                continue;
            }

            if (Hepburn.TryGetValue(ch, out var romaji))
            {
                var alternatives = romaji.ToList();
                if (anySystem && OtherSystems.TryGetValue(ch, out var other))
                    alternatives.AddRange(other);

                var unit = new RomanisationUnit(original, alternatives);
                if (previous?.Source != null && "あいうえお".Contains(ch))
                    AddLongVowelAlternatives(previous, unit, romaji[0][0]);
                AddUnit(unit);
                continue;
            }

            if (SmallY.TryGetValue(ch, out var yVowel))
            {
                if (previous?.Source != null && previous.Alternatives.All(alt => alt.EndsWith('i')))
                    previous.Alternatives = previous
                        .Alternatives.SelectMany(alt => YoonAlternatives(alt, yVowel, anySystem))
                        .Distinct()
                        .ToList();
                else
                    AddUnit(new RomanisationUnit(original, ["y" + yVowel]));
                continue;
            }

            if (SmallVowels.TryGetValue(ch, out var vowel))
            {
                if (previous?.Source != null && previous.Alternatives.All(alt => alt.Length > 0))
                    previous.Alternatives = previous
                        .Alternatives.SelectMany(alt => SmallVowelAlternatives(alt, vowel))
                        .Distinct()
                        .ToList();
                else
                    AddUnit(new RomanisationUnit(original, [vowel.ToString()]));
                continue;
            }

            if (ch == SmallTsu)
            {
                FlushTsu();
                pendingTsu = true;
                adjacent = true;
                continue;
            }

            if (
                ch == LongVowelMark
                && previous?.Source != null
                && previous.Alternatives[0].Length > 0
            )
            {
                var last = previous.Alternatives[0][^1];
                var alternatives = new List<string> { "" };
                if (Vowels.Contains(last))
                    alternatives.Insert(0, last.ToString());
                if (last == 'o')
                    alternatives.Add("h");
                AddUnit(new RomanisationUnit(original, alternatives));
                continue;
            }

            // Kanji, iteration marks and other scripts can't be romanised here.
            if (char.IsLetter(ch) || IsKana(ch))
                return null;

            FlushTsu();
            adjacent = false;
        }

        FlushTsu();
        return units;

        void AddUnit(RomanisationUnit unit)
        {
            if (pendingTsu)
            {
                unit.Alternatives = unit
                    .Alternatives.SelectMany(alt => Geminate(alt, anySystem))
                    .Distinct()
                    .ToList();
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
            units.Add(new RomanisationUnit(SmallTsu, ["", "t", "tsu"]));
        }
    }

    /// <summary> きゃ → kya, しゃ → sha, ちゃ → cha, じゃ → ja. </summary>
    private static IEnumerable<string> YoonAlternatives(string alt, char vowel, bool anySystem)
    {
        var stem = alt[..^1];
        if (alt.EndsWith("shi") || alt.EndsWith("chi") || alt.EndsWith("ji"))
            yield return stem + vowel;
        else
            yield return stem + "y" + vowel;

        // Kunrei-shiki writes じゃ as "zya", and typing habits give "jya".
        if (anySystem)
            yield return stem + "y" + vowel;
    }

    /// <summary> ふぁ → fa, てぃ → ti, ウィ → wi, イェ → ye, クァ → kwa, or both sounds written out. </summary>
    private static IEnumerable<string> SmallVowelAlternatives(string alt, char vowel)
    {
        var stem = alt[..^1];
        yield return stem + vowel;
        if (alt.EndsWith('u'))
            yield return stem + "w" + vowel;
        if (alt == "i")
            yield return "y" + vowel;
        yield return alt + vowel;
    }

    /// <summary> っか → kka, っち → tchi. </summary>
    private static IEnumerable<string> Geminate(string romaji, bool anySystem)
    {
        if (romaji.StartsWith("ch"))
        {
            yield return "t" + romaji;
            if (anySystem)
                yield return "c" + romaji;
        }
        else if (romaji.Length > 0 && !Vowels.Contains(romaji[0]))
            yield return romaji[0] + romaji;
        else
            yield return romaji;
    }

    private static void AddLongVowelAlternatives(
        RomanisationUnit previous,
        RomanisationUnit unit,
        char vowel
    )
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

    private static bool IsHiragana(char ch) => ch is >= 'ぁ' and <= 'ゟ';

    private static bool IsKatakana(char ch) => ch is >= 'ァ' and <= 'ヺ' or 'ヽ' or 'ヾ';

    private static bool IsKana(char ch) => IsHiragana(ch) || IsKatakana(ch);

    private static char ToHiragana(char ch) => ch is >= 'ァ' and <= 'ヶ' ? (char)(ch - 0x60) : ch;
}
