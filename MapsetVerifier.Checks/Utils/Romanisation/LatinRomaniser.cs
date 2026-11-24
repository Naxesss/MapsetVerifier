using System.Globalization;
using System.Text;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Latin letters with diacritics, romanised with the Ranking Criteria's letter rules for German, Nordic, Swedish
///     and Finnish. Letters those rules don't cover become their nearest ASCII letter (é → e).
/// </summary>
public sealed class LatinRomaniser : LetterTableRomaniser
{
    /// <summary> Strict spellings, and other spellings of the same letters that point at a wrong rule. </summary>
    private sealed record Spelling(string[] Strict, string[] Other);

    public static readonly LatinRomaniser German = new(
        "German",
        "the German letter rules",
        [RC.Metadata.Romanisation_GermanUUeOOe],
        ["german"],
        new()
        {
            ['ä'] = new(["ae"], ["a"]),
            ['ö'] = new(["oe"], ["o"]),
            ['ü'] = new(["ue"], ["u"]),
            ['ß'] = new(["ss"], ["s"]),
        }
    );

    public static readonly LatinRomaniser Swedish = new(
        "Swedish",
        "the Swedish letter rules",
        [RC.Metadata.Romanisation_SwedishOO],
        ["swedish"],
        new()
        {
            ['ö'] = new(["o"], ["oe"]),
            ['ä'] = new(["a"], ["ae"]),
            ['å'] = new(["a"], ["aa"]),
        }
    );

    public static readonly LatinRomaniser Finnish = new(
        "Finnish",
        "the Finnish letter rules",
        [RC.Metadata.Romanisation_FinnishOO],
        ["finnish"],
        new()
        {
            ['ö'] = new(["o"], ["oe"]),
            ['ä'] = new(["a"], ["ae"]),
            ['å'] = new(["a"], ["aa"]),
        }
    );

    public static readonly LatinRomaniser Nordic = new(
        "Nordic",
        "the Nordic letter rules",
        [RC.Metadata.Romanisation_NordicAeOeAa],
        ["danish", "norwegian", "nordic"],
        new()
        {
            ['æ'] = new(["ae"], ["a", "e"]),
            ['ø'] = new(["oe"], ["o"]),
            ['å'] = new(["aa"], ["a"]),
        }
    );

    /// <summary>
    ///     For beatmapsets without one of the language tags above. Any of the four languages' spellings is
    ///     accepted, so only spellings none of them allow are reported, like ø as o.
    /// </summary>
    public static readonly LatinRomaniser AnyLanguage = new(
        "Latin",
        "the German, Nordic, Swedish and Finnish letter rules",
        [
            RC.Metadata.Romanisation_GermanUUeOOe,
            RC.Metadata.Romanisation_NordicAeOeAa,
            RC.Metadata.Romanisation_SwedishOO,
            RC.Metadata.Romanisation_FinnishOO,
        ],
        [],
        new()
        {
            ['ä'] = new(["ae", "a"], []),
            ['ö'] = new(["oe", "o"], []),
            ['å'] = new(["aa", "a"], []),
            // Also u in languages the rules don't cover, like Turkish and Spanish.
            ['ü'] = new(["ue", "u"], []),
            ['ß'] = new(["ss"], ["s"]),
            ['æ'] = new(["ae"], ["a", "e"]),
            ['ø'] = new(["oe"], ["o"]),
        }
    );

    // Letters that don't decompose into an ASCII letter with a mark.
    private static readonly Dictionary<char, string> NearestAscii = new()
    {
        ['æ'] = "ae",
        ['ø'] = "o",
        ['œ'] = "oe",
        ['ß'] = "ss",
        ['ł'] = "l",
        ['đ'] = "d",
        ['ð'] = "d",
        ['þ'] = "th",
        ['ı'] = "i",
        ['ħ'] = "h",
    };

    private readonly Dictionary<char, Spelling> spellings;

    private LatinRomaniser(
        string name,
        string systemName,
        string[] rules,
        string[] requiredTags,
        Dictionary<char, Spelling> spellings
    )
    {
        Name = name;
        SystemName = systemName;
        Rules = rules;
        RequiredTags = requiredTags;
        this.spellings = spellings;
    }

    public override string Name { get; }
    private string SystemName { get; }
    public override string[] Rules { get; }
    public override string[] RequiredTags { get; }

    public override string Cause =>
        "The unicode artist or title has letters like ä, ö, ü, å, æ or ø, and the romanised field spells them in a way "
        + SystemName
        + " don't allow.";

    // Latin-1 Supplement, Latin Extended-A and -B, and Latin Extended Additional.
    protected override bool IsAlphabetLetter(char letter) =>
        char.IsLetter(letter) && (letter is >= 'À' and <= 'ɏ' or >= 'Ḁ' and <= 'ỿ');

    protected override string[]? Romanise(char letter, char? previous, bool anySystem, string text)
    {
        if (spellings.TryGetValue(letter, out var spelling))
            return anySystem ? [.. spelling.Strict, .. spelling.Other] : spelling.Strict;

        var nearest = Nearest(letter);
        return nearest == null ? null : [nearest];
    }

    /// <summary> The nearest ASCII spelling of a letter, by dropping its marks (é → e). </summary>
    private static string? Nearest(char letter)
    {
        if (NearestAscii.TryGetValue(letter, out var nearest))
            return nearest;

        var stripped = new string(
            letter
                .ToString()
                .Normalize(NormalizationForm.FormD)
                .Where(ch =>
                    CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark
                )
                .ToArray()
        );
        return stripped.Length == 1 && stripped[0] is >= 'a' and <= 'z' ? stripped : null;
    }
}
