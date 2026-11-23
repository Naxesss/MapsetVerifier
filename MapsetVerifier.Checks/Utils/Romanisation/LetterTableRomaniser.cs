namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     A romaniser for alphabets that romanise letter by letter, from a table. Like Japanese, a mismatch is only
///     reported when another common spelling of the same letters fits instead, since the romanised field may
///     also be an official name or translation.
/// </summary>
public abstract class LetterTableRomaniser : Romaniser
{
    protected override bool HasLetterCase => true;

    /// <summary> Whether the lowercase letter belongs to this romaniser's alphabet. </summary>
    protected abstract bool IsAlphabetLetter(char letter);

    /// <summary>
    ///     Accepted romanisations of a lowercase letter, the preferred one first, or null when it can't be read.
    ///     <paramref name="previous" /> is the letter before it in the same word, if any.
    /// </summary>
    protected abstract string[]? Romanise(char letter, char? previous, bool anySystem);

    protected override bool ContainsScript(string text) =>
        text.Any(ch => ch >= 128 && IsAlphabetLetter(char.ToLowerInvariant(ch)));

    protected override List<RomanisationUnit>? Read(string text) => Read(text, anySystem: false);

    protected override List<RomanisationUnit>? ReadAnySystem(string text) =>
        Read(text, anySystem: true);

    protected override bool MayUseDifferentWords(string text) => true;

    private List<RomanisationUnit>? Read(string text, bool anySystem)
    {
        var units = new List<RomanisationUnit>();
        char? previous = null;

        foreach (var ch in text)
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
                var alternatives = Romanise(letter, previous, anySystem);
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
