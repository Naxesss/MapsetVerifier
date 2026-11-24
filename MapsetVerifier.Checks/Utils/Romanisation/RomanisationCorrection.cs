using System.Text;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary> A romanised field with its letters corrected, and how many edits that took. A swap of letters is one. </summary>
public sealed record RomanisationCorrection(string Text, int Edits)
{
    /// <summary> Every way to swap two letters next to each other, with capitals staying in place. </summary>
    public static IEnumerable<RomanisationCorrection> Swaps(string romanised)
    {
        for (var i = 0; i + 1 < romanised.Length; i++)
        {
            var (first, second) = (romanised[i], romanised[i + 1]);
            if (!char.IsAsciiLetter(first) || !char.IsAsciiLetter(second))
                continue;

            var swapped = romanised.ToCharArray();
            swapped[i] = WithCaseOf(second, first);
            swapped[i + 1] = WithCaseOf(first, second);
            yield return new RomanisationCorrection(new string(swapped), 1);
        }
    }

    private static char WithCaseOf(char letter, char other) =>
        char.IsUpper(other) ? char.ToUpperInvariant(letter) : char.ToLowerInvariant(letter);

    private static bool IsVowel(char letter) => "aeiou".Contains(letter);

    /// <summary>
    ///     Changes the letters of a romanised field into the given ones, with as few edits as possible. Spaces,
    ///     punctuation and capitals stay as they are.
    /// </summary>
    /// <param name="romanised"> The romanised field. </param>
    /// <param name="words"> The correct letters in lowercase, as separate words where those are known. </param>
    public static RomanisationCorrection Apply(string romanised, IReadOnlyList<string> words)
    {
        var letters = string.Concat(words);
        var wordEnds = new HashSet<int>();
        foreach (var word in words)
            wordEnds.Add(wordEnds.DefaultIfEmpty(0).Max() + word.Length);

        // Where the letters are in the romanised field.
        var positions = Enumerable
            .Range(0, romanised.Length)
            .Where(position => char.IsAsciiLetter(romanised[position]))
            .ToList();

        // Edits needed from each letter of both onwards.
        var edits = new int[positions.Count + 1, letters.Length + 1];
        for (var i = positions.Count; i >= 0; i--)
        for (var j = letters.Length; j >= 0; j--)
            edits[i, j] =
                i == positions.Count ? letters.Length - j
                : j == letters.Length ? positions.Count - i
                : Math.Min(
                    Math.Min(edits[i + 1, j], edits[i, j + 1]) + 1,
                    edits[i + 1, j + 1] + (LetterAt(i) == letters[j] ? 0 : 1)
                );

        var result = new StringBuilder();
        var copied = 0;
        var capitaliseNext = false;
        var lowerNext = false;

        for (int i = 0, j = 0; i < positions.Count || j < letters.Length; )
        {
            var isSame = i < positions.Count && j < letters.Length && LetterAt(i) == letters[j];

            if (isSame && edits[i, j] == edits[i + 1, j + 1])
            {
                Copy(positions[i]);
                Append(letters[j], char.IsUpper(romanised[positions[i]]));
                copied = positions[i] + 1;
                i++;
                j++;
            }
            else if (i < positions.Count && edits[i, j] == edits[i + 1, j] + 1)
            {
                // Left out. A word that loses its capital letter gets it back on the next one.
                Copy(positions[i]);
                capitaliseNext =
                    capitaliseNext || (StartsWord(i) && char.IsUpper(romanised[positions[i]]));
                copied = positions[i] + 1;
                i++;
            }
            else if (j < letters.Length && edits[i, j] == edits[i, j + 1] + 1)
            {
                // Added. Between two words it belongs to the next one, unless a word is known to end with it.
                var startsNext =
                    i < positions.Count
                    && StartsWord(i)
                    && !wordEnds.Contains(j + 1)
                    && !(IsVowel(letters[j]) && NeedsVowel(i - 1));
                if (startsNext)
                {
                    Copy(positions[i]);
                    copied = positions[i];
                }

                // The capital a word starts with moves along to its new first letter.
                var takesCapital =
                    startsNext
                    && (
                        char.IsUpper(romanised[positions[i]])
                        || (i == 0 && romanised.Any(char.IsAsciiLetterUpper))
                    );
                Append(letters[j], takesCapital || IsInCapitals(i, startsNext));
                lowerNext = takesCapital && !IsInCapitals(i, startsNext);
                j++;
            }
            else
            {
                Copy(positions[i]);
                Append(letters[j], char.IsUpper(romanised[positions[i]]));
                copied = positions[i] + 1;
                i++;
                j++;
            }
        }

        result.Append(romanised, copied, romanised.Length - copied);
        return new RomanisationCorrection(result.ToString(), edits[0, 0]);

        char LetterAt(int index) => char.ToLowerInvariant(romanised[positions[index]]);

        bool StartsWord(int index) =>
            positions[index] == 0 || !char.IsAsciiLetter(romanised[positions[index] - 1]);

        // Japanese words end with a vowel or an n.
        bool NeedsVowel(int index) =>
            index >= 0
            && !IsVowel(LetterAt(index))
            && (LetterAt(index) != 'n' || StartsWord(index));

        void Copy(int until)
        {
            if (until > copied)
                result.Append(romanised, copied, until - copied);
        }

        void Append(char letter, bool isUpper)
        {
            result.Append(
                (isUpper && !lowerNext) || capitaliseNext ? char.ToUpperInvariant(letter) : letter
            );
            capitaliseNext = false;
            lowerNext = false;
        }

        // Added letters are capitals when the letters around them in the word are.
        bool IsInCapitals(int index, bool startsNext)
        {
            var around = startsNext ? new[] { index, index + 1 } : new[] { index - 2, index - 1 };

            return around.All(other =>
                other >= 0 && other < positions.Count && char.IsUpper(romanised[positions[other]])
            );
        }
    }
}
