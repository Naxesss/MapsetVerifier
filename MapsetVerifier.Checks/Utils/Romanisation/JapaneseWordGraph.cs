namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Every way to split Japanese text into words, and every way to romanise those: kana as it sounds, words and names
///     with kanji in any of their readings, and loan words in the spelling of their source language.
/// </summary>
public sealed class JapaneseWordGraph
{
    /// <summary> The words which may start at each character. </summary>
    private readonly List<RomanisationWord>[] words;

    /// <param name="text"> The text, already normalised. </param>
    /// <param name="readKana"> Reads kana into units, or null if it can't be read. </param>
    public JapaneseWordGraph(string text, Func<string, List<RomanisationUnit>?> readKana)
    {
        words = new List<RomanisationWord>[text.Length];
        for (var start = 0; start < text.Length; start++)
            words[start] = FindWords(text, start, readKana);

        // The analyzer also knows how words are conjugated, which the dictionary doesn't.
        var position = 0;
        foreach (var word in JapaneseReader.Read(text))
        {
            if (word.HasKanji && word.Kana != null && readKana(word.Kana) is { } units)
                words[position].Add(new RomanisationWord(position + word.Text.Length, units));
            position += word.Text.Length;
        }
    }

    /// <summary> Whether the letters are a romanisation of the whole text. </summary>
    public bool Matches(string target) => EndsFrom(0, target).Contains(words.Length);

    /// <summary> The closest romanisation of the text to the letters, by its words, or null if it can't be read. </summary>
    public IReadOnlyList<string>? FindClosest(string target) =>
        RomanisationSearch.FindClosest(words, target);

    /// <summary> Where the text may have got to once the letters are used up, starting at the given character. </summary>
    private List<int> EndsFrom(int start, string target)
    {
        var offsets = new HashSet<int>?[words.Length + 1];
        offsets[start] = [0];

        for (var position = start; position < words.Length; position++)
        {
            if (offsets[position] is not { } reached)
                continue;

            foreach (var word in words[position])
            {
                var ends = reached
                    .SelectMany(offset => Romaniser.EndsOf(word.Units, target, offset))
                    .ToList();
                if (ends.Count > 0)
                    (offsets[word.End] ??= []).UnionWith(ends);
            }
        }

        return Enumerable
            .Range(start, words.Length + 1 - start)
            .Where(position => offsets[position]?.Contains(target.Length) == true)
            .ToList();
    }

    private static List<RomanisationWord> FindWords(
        string text,
        int start,
        Func<string, List<RomanisationUnit>?> readKana
    )
    {
        var found = new List<RomanisationWord>();
        var first = text[start];

        if (first < 128 || !IsJapanese(first))
        {
            // Separators aren't compared. A cross between names is often written as a letter (A × B → A x B).
            if (first == '×')
                found.Add(new RomanisationWord(start + 1, [new RomanisationUnit(null, ["", "x"])]));
            else if (first < 128 && char.IsLetter(first))
                found.Add(
                    new RomanisationWord(
                        start + 1,
                        [Letters(char.ToLowerInvariant(first).ToString())]
                    )
                );
            else if (!char.IsLetter(first))
                found.Add(new RomanisationWord(start + 1, []));

            return found;
        }

        for (
            var end = start + 1;
            end <= text.Length
                && end - start <= JapaneseDictionary.MaxWordLength
                && IsJapanese(text[end - 1]);
            end++
        )
        {
            var word = text[start..end];

            if (word.All(IsKana))
            {
                if (readKana(word) is { } units)
                    found.Add(new RomanisationWord(end, units));
                if (FindSpellings(word) is { Count: > 0 } spellings)
                    found.Add(new RomanisationWord(end, [new RomanisationUnit(null, spellings)]));
            }
            else
                found.AddRange(
                    JapaneseDictionary
                        .Find(word)
                        .Select(readKana)
                        .OfType<List<RomanisationUnit>>()
                        .Select(units => new RomanisationWord(end, units))
                );
        }

        return found;
    }

    /// <summary> How a loan word is spelled in its source language (ハート → heart), also as plural (デイズ → days). </summary>
    private static List<string> FindSpellings(string kana)
    {
        var key = new string(kana.Where(ch => ch is not 'ー').Select(ToKatakana).ToArray());
        if (key.Length < 2)
            return [];

        var spellings = JapaneseDictionary.Find(key).ToList();
        if (key.Length > 2 && key[^1] is 'ズ' or 'ス')
            spellings.AddRange(
                JapaneseDictionary
                    .Find(key[..^1])
                    .SelectMany(word =>
                        word.EndsWith('y') ? new[] { word + "s", word[..^1] + "ies" } : [word + "s"]
                    )
            );

        return spellings.Distinct().ToList();
    }

    private static RomanisationUnit Letters(string letters) => new(null, [letters]);

    private static char ToKatakana(char ch) => ch is >= 'ぁ' and <= 'ゖ' ? (char)(ch + 0x60) : ch;

    private static bool IsKana(char ch) =>
        ch is >= 'ぁ' and <= 'ゟ' or >= 'ァ' and <= 'ヺ' or 'ー' or 'ヽ' or 'ヾ';

    private static bool IsJapanese(char ch) =>
        IsKana(ch) || JapaneseReader.ContainsKanji(ch.ToString());
}
