namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     A romaniser for alphabets that romanise letter by letter, from a table. Like Japanese, a mismatch is only certain
///     when another common spelling of the same letters fits instead, since the romanised field may also be an
///     official name or translation.
/// </summary>
public abstract class LetterTableRomaniser : Romaniser
{
    protected override bool HasLetterCase => true;

    /// <summary> Whether the lowercase letter belongs to this romaniser's alphabet. </summary>
    protected abstract bool IsAlphabetLetter(char letter);

    /// <summary>
    ///     Accepted romanisations of a lowercase letter, the preferred one first, or null when it can't be read.
    ///     <paramref name="previous" /> is the letter before it in the same word, if any, and <paramref name="text" /> is
    ///     the whole text, which may tell the language.
    /// </summary>
    protected abstract string[]? Romanise(char letter, char? previous, bool anySystem, string text);

    protected override bool ContainsScript(string text) =>
        text.Any(ch => ch >= 128 && IsAlphabetLetter(char.ToLowerInvariant(ch)));

    protected override List<RomanisationUnit>? Read(string text) =>
        Read(text, anySystem: false, text);

    protected override List<RomanisationUnit>? ReadAnySystem(string text) =>
        Read(text, anySystem: true, text);

    protected override bool MayUseDifferentWords(string text) => true;

    public override bool PointsOutInconclusive(string unicode, IReadOnlyCollection<string> tags) =>
        true;

    protected override List<List<RomanisationUnit>>? ReadWords(string text) =>
        ReadWords(text, word => Read(word, anySystem: false, text));

    private List<RomanisationUnit>? Read(string part, bool anySystem, string text)
    {
        var units = new List<RomanisationUnit>();
        char? previous = null;

        foreach (var ch in part)
        {
            var letter = char.ToLowerInvariant(ch);
            if (ch < 128 && char.IsLetter(ch))
            {
                units.Add(
                    new RomanisationUnit(null, [letter.ToString()])
                    {
                        IsUppercase = char.IsUpper(ch),
                    }
                );
                previous = letter;
                continue;
            }

            if (ch >= 128 && IsAlphabetLetter(letter))
            {
                var alternatives = Romanise(letter, previous, anySystem, text);
                if (alternatives == null)
                    return null;

                units.Add(
                    new RomanisationUnit(ch, [.. alternatives]) { IsUppercase = char.IsUpper(ch) }
                );
                previous = letter;
                continue;
            }

            // Letters of other scripts can't be read here.
            if (char.IsLetter(ch))
                return null;

            previous = null;
        }

        return units;
    }
}
