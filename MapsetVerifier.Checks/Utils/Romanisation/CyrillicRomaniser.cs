using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Cyrillic in the BGN/PCGN system. The language isn't always known, so the Russian, Ukrainian and Belarusian
///     readings of shared letters are all accepted (г as g or h, и as i or y). Ukrainian, Bulgarian, Serbian and
///     Macedonian have systems of their own, which are accepted as well if the text has letters or spellings that only
///     those languages use. Letters those systems write with diacritics are expected as their plain letter.
/// </summary>
public sealed class CyrillicRomaniser : LetterTableRomaniser
{
    private static readonly Dictionary<char, string[]> Bgn = new()
    {
        ['а'] = ["a"],
        ['б'] = ["b"],
        ['в'] = ["v"],
        ['г'] = ["g", "h"],
        ['д'] = ["d"],
        // е and ё are handled in Romanise, since they depend on the letter before.
        ['ж'] = ["zh"],
        ['з'] = ["z"],
        ['и'] = ["i", "y"],
        ['й'] = ["y"],
        ['к'] = ["k"],
        ['л'] = ["l"],
        ['м'] = ["m"],
        ['н'] = ["n"],
        ['о'] = ["o"],
        ['п'] = ["p"],
        ['р'] = ["r"],
        ['с'] = ["s"],
        ['т'] = ["t"],
        ['у'] = ["u"],
        ['ф'] = ["f"],
        ['х'] = ["kh"],
        ['ц'] = ["ts"],
        ['ч'] = ["ch"],
        ['ш'] = ["sh"],
        ['щ'] = ["shch"],
        // The hard and soft signs are written as apostrophes, which an ASCII field may leave out.
        ['ъ'] = [""],
        ['ы'] = ["y"],
        ['ь'] = [""],
        ['э'] = ["e"],
        ['ю'] = ["yu"],
        ['я'] = ["ya"],
        // Ukrainian and Belarusian.
        ['є'] = ["ye"],
        ['і'] = ["i"],
        ['ї'] = ["yi"],
        ['ґ'] = ["g"],
        ['ў'] = ["w"],
        // Serbian and Macedonian.
        ['ђ'] = ["dj"],
        ['ј'] = ["j"],
        ['љ'] = ["lj"],
        ['њ'] = ["nj"],
        ['ћ'] = ["c"],
        ['џ'] = ["dz"],
        ['ѓ'] = ["gj"],
        ['ќ'] = ["kj"],
        ['ѕ'] = ["dz"],
        // Pre-reform Russian, as in older titles and stylised names.
        ['ѣ'] = ["e", "ye"],
        ['ѳ'] = ["f"],
        ['ѵ'] = ["i"],
    };

    private const string UkrainianLetters = "іїєґ";
    private const string NotUkrainianLetters = "ыэёъўѣ";
    private const string SouthSlavicLetters = "ђјљњћџѓќѕ";

    // Letters which Serbian and Macedonian write differently.
    private static readonly Dictionary<char, string[]> SouthSlavic = new()
    {
        ['ж'] = ["z"],
        ['х'] = ["h"],
        ['ц'] = ["c"],
        ['ч'] = ["c"],
        ['ш'] = ["s"],
        ['ђ'] = ["d"],
        ['ѓ'] = ["g"],
        ['ќ'] = ["k"],
    };

    private static readonly Dictionary<char, string[]> Bulgarian = new()
    {
        ['х'] = ["h"],
        ['щ'] = ["sht"],
        ['ъ'] = ["a"],
        ['ь'] = ["y"],
    };

    // Spellings from other systems (scientific, ISO 9, German and Polish style), which aren't BGN/PCGN.
    private static readonly Dictionary<char, string[]> OtherSystems = new()
    {
        ['в'] = ["w"],
        ['ж'] = ["j", "z", "zs"],
        ['й'] = ["j", "i"],
        ['х'] = ["h", "x", "ch"],
        ['ц'] = ["c", "tz", "z"],
        ['ч'] = ["c", "tch", "tsch", "cz"],
        ['ш'] = ["s", "sch", "sz"],
        ['щ'] = ["sc", "shh", "sch", "sht", "szcz", "shtsh"],
        ['ы'] = ["i"],
        ['ь'] = ["j"],
        ['ю'] = ["ju", "iu"],
        ['я'] = ["ja", "ia"],
        ['е'] = ["je"],
        ['ё'] = ["jo", "io"],
        ['є'] = ["je"],
        ['ї'] = ["ji"],
    };

    public override string Name => "Cyrillic";
    public override string[] Rules => [RC.Metadata.Romanisation_CyrillicWritingUseBgnPcgn];

    public override string Cause =>
        "The unicode artist or title is written in Cyrillic, and the romanised field follows another system like the scientific or German one instead of BGN/PCGN.";

    protected override bool IsAlphabetLetter(char letter) => letter is >= 'Ѐ' and <= 'ӿ';

    protected override string[]? Romanise(char letter, char? previous, bool anySystem, string text)
    {
        string[]? strict;
        if (letter is 'е' or 'ё')
        {
            // Written ye at the start of a word and after vowels and signs, e elsewhere; both are accepted.
            var startsSyllable = previous == null || "аеёиоуыэюяєіїйъь".Contains(previous.Value);
            strict = startsSyllable ? ["ye", "e"] : ["e", "ye"];
            if (letter == 'ё')
                strict = [.. strict, "yo", "o"];
        }
        else if (!Bgn.TryGetValue(letter, out strict))
            return null;

        // The spellings of a known language are preferred.
        strict = [.. RomaniseByLanguage(letter, previous, text).Concat(strict).Distinct()];

        return anySystem && OtherSystems.TryGetValue(letter, out var other)
            ? [.. strict, .. other]
            : strict;
    }

    /// <summary> How a letter is romanised in the language the text is in, as far as that can be told and it differs. </summary>
    private static string[] RomaniseByLanguage(char letter, char? previous, string text)
    {
        if (text.Any(ch => SouthSlavicLetters.Contains(char.ToLowerInvariant(ch))))
            return SouthSlavic.GetValueOrDefault(letter) ?? [];

        if (IsBulgarian(text))
            return letter == 'я' && previous == 'и'
                ? ["ia"]
                : Bulgarian.GetValueOrDefault(letter) ?? [];

        if (!IsUkrainian(text))
            return [];

        return letter switch
        {
            'г' => previous == 'з' ? ["gh", "h"] : ["h"],
            'е' => ["e"],
            'и' => ["y"],
            'є' => previous == null ? ["ye"] : ["ie"],
            'ї' => previous == null ? ["yi", "i"] : ["i"],
            'й' => previous == null ? ["y"] : ["i"],
            'ю' => previous == null ? ["yu", "iu"] : ["iu"],
            'я' => previous == null ? ["ya", "ia"] : ["ia"],
            _ => [],
        };
    }

    private static bool IsUkrainian(string text)
    {
        var letters = text.ToLowerInvariant();

        return letters.Any(ch => UkrainianLetters.Contains(ch))
            && !letters.Any(ch => NotUkrainianLetters.Contains(ch));
    }

    /// <summary> Russian only has the hard sign before some vowels, and at the end of words in old spelling. </summary>
    private static bool IsBulgarian(string text)
    {
        var letters = text.ToLowerInvariant();

        return Enumerable
            .Range(0, letters.Length)
            .Any(i =>
                letters[i] == 'ъ'
                && i + 1 < letters.Length
                && char.IsLetter(letters[i + 1])
                && !"еёюя".Contains(letters[i + 1])
            );
    }
}
