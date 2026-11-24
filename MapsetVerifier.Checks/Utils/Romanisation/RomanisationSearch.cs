namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary> A word of the text, and how it's romanised. </summary>
/// <param name="End"> The position right after the word. </param>
/// <param name="Units"> The units of the word. </param>
public sealed record RomanisationWord(int End, List<RomanisationUnit> Units);

/// <summary> Finds the accepted romanisation that's closest to what was written. </summary>
public static class RomanisationSearch
{
    /// <summary>
    ///     The romanisation of the text with the fewest letters different from the given ones, by its words, or null if
    ///     it can't be read. Of those equally close, the preferred romanisation is picked.
    /// </summary>
    /// <param name="words"> The words which may start at each position of the text. </param>
    /// <param name="target"> The letters to come close to. </param>
    public static IReadOnlyList<string>? FindClosest(
        IReadOnlyList<IReadOnlyList<RomanisationWord>> words,
        string target
    )
    {
        // For each character reached, the best way to get there for each number of letters used up.
        var ways = new Way?[words.Count + 1][];
        for (var position = 0; position <= words.Count; position++)
            ways[position] = new Way?[target.Length + 1];
        for (var used = 0; used <= target.Length; used++)
            ways[0][used] = new Way(used * EditCost, "", null);

        for (var position = 0; position < words.Count; position++)
            foreach (var word in words[position])
            {
                var current = ways[position].Select(way => way?.StartWord()).ToArray();
                foreach (var unit in word.Units)
                {
                    var next = new Way?[target.Length + 1];
                    for (var index = 0; index < unit.Alternatives.Count; index++)
                        Improve(next, Spell(current, unit.Alternatives[index], target, index));
                    current = next;
                }

                Improve(ways[word.End], current);
            }

        var spelled = new List<string>();
        for (var way = ways[^1][^1]; way != null; way = way.Previous)
            if (way.Word.Length > 0)
                spelled.Insert(0, way.Word);

        return ways[^1][^1] == null ? null : spelled;
    }

    /// <summary> An edit outweighs any preference between romanisations. </summary>
    private const int EditCost = 1000;

    /// <param name="Cost"> The edits so far, and how far from preferred the romanisation is. </param>
    /// <param name="Word"> The letters of the last word so far. </param>
    /// <param name="Previous"> The way up to the last word. </param>
    private sealed record Way(int Cost, string Word, Way? Previous)
    {
        public Way StartWord() => new(Cost, "", this);
    }

    /// <summary> Adds letters to each way, for every number of letters used up after it. </summary>
    private static Way?[] Spell(Way?[] from, string letters, string target, int preference)
    {
        var current = from.Select(way =>
                way == null
                    ? null
                    : way with
                    {
                        Cost = way.Cost + preference,
                        Word = way.Word + letters,
                    }
            )
            .ToArray();

        foreach (var letter in letters)
        {
            var next = new Way?[target.Length + 1];
            for (var used = 0; used <= target.Length; used++)
            {
                Offer(next, used, current[used], EditCost);
                if (used == 0)
                    continue;

                Offer(next, used, current[used - 1], target[used - 1] == letter ? 0 : EditCost);
                Offer(next, used, next[used - 1], EditCost);
            }

            current = next;
        }

        return current;
    }

    private static void Offer(Way?[] ways, int used, Way? way, int cost)
    {
        if (way != null && (ways[used] == null || way.Cost + cost < ways[used]!.Cost))
            ways[used] = way with { Cost = way.Cost + cost };
    }

    private static void Improve(Way?[] ways, Way?[] others)
    {
        for (var used = 0; used < ways.Length; used++)
            Offer(ways, used, others[used], 0);
    }
}
