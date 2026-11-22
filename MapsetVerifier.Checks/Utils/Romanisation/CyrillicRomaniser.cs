using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Cyrillic in the BGN/PCGN system. The language isn't known, so the Russian, Ukrainian and Belarusian readings
///     of shared letters are all accepted (г as g or h, и as i or y).
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
    public override string SystemName => "BGN/PCGN";
    public override string[] Rules => [RC.Metadata.Romanisation_CyrillicWritingUseBgnPcgn];

    public override string Cause =>
        "The unicode artist or title is written in Cyrillic, and the romanised field follows another system like the scientific or German one instead of BGN/PCGN.";

    protected override bool IsAlphabetLetter(char letter) => letter is >= 'Ѐ' and <= 'ӿ';

    protected override string[]? Romanise(char letter, char? previous, bool anySystem)
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

        return anySystem && OtherSystems.TryGetValue(letter, out var other)
            ? [.. strict, .. other]
            : strict;
    }
}
