using System.Text;
using System.Text.RegularExpressions;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     Chinese in pinyin, for beatmapsets tagged as Chinese. Every reading of a character is accepted, since the
///     right one depends on the word. The Ranking Criteria rule for Chinese is about format: each character is
///     its own capitalised word, except for proper nouns. That's what gets reported; a romanised field that doesn't
///     fit the pinyin at all, like a translation or Cantonese, isn't.
/// </summary>
public sealed partial class ChineseRomaniser : Romaniser
{
    private static readonly Lazy<Dictionary<string, string[]>> Readings = new(LoadReadings);

    public override string Name => "Chinese";
    public override string[] Rules =>
        [RC.Metadata.Romanisation_ChineseCharacterRomanisedCapitalisedWord];
    public override string[] RequiredTags => ["chinese"];

    public override string Cause =>
        "The beatmapset is tagged as Chinese, and the romanised field follows the pinyin of the unicode field but doesn't write each character as its own capitalised word.";

    public override string? FormatMessage => "{0} \"{1}\" should be \"{2}\", unless a proper noun.";

    protected override bool ContainsScript(string text) =>
        text.EnumerateRunes().Any(rune => IsHan(rune));

    // A different romanisation can't be told apart from a translation or another Chinese language.
    protected override bool MayUseDifferentWords(string text) => true;

    protected override List<RomanisationUnit>? Read(string text)
    {
        var units = new List<RomanisationUnit>();
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.IsAscii)
            {
                if (Rune.IsLetter(rune))
                    units.Add(
                        new RomanisationUnit(
                            null,
                            [char.ToLowerInvariant((char)rune.Value).ToString()]
                        )
                    );
                continue;
            }

            if (IsHan(rune))
            {
                if (!Readings.Value.TryGetValue(rune.ToString(), out var readings))
                    return null;

                // Each character is its own word.
                units.Add(
                    new RomanisationUnit(
                        rune.ToString()[0],
                        [.. readings.SelectMany(WithoutUmlaut).Distinct()]
                    )
                    {
                        StartsWord = true,
                    }
                );
                continue;
            }

            if (Rune.IsLetter(rune))
                return null;
        }

        return units;
    }

    /// <summary>
    ///     Whether every character is romanised as its own capitalised word, like "Wo Ai Ni" for 我爱你. Only called
    ///     when the letters already match, so this is about spacing and capitals alone.
    /// </summary>
    public override bool FollowsFormat(string unicode, string romanised)
    {
        var units = Read(Normalise(unicode));
        if (units == null)
            return true;

        // The letters of the romanised field, and which of them start a word in it.
        var letters = new StringBuilder();
        var wordStarts = new Dictionary<int, bool>();
        foreach (Match word in Word().Matches(romanised))
        {
            wordStarts[letters.Length] = char.IsUpper(word.Value[0]);
            letters.Append(word.Value.ToLowerInvariant());
        }

        var target = letters.ToString();
        bool IsWordBoundary(int position) =>
            position == target.Length || wordStarts.ContainsKey(position);

        // Positions reachable after each unit, where a character must start a capitalised word and end at a word
        // boundary.
        var positions = new HashSet<int> { 0 };
        foreach (var unit in units)
        {
            var next = new HashSet<int>();
            foreach (var position in positions)
            foreach (var alternative in unit.Alternatives)
            {
                if (!target.AsSpan(position).StartsWith(alternative))
                    continue;

                var end = position + alternative.Length;
                if (
                    unit.Source != null
                    && (
                        !wordStarts.TryGetValue(position, out var capitalised)
                        || !capitalised
                        || !IsWordBoundary(end)
                    )
                )
                    continue;

                next.Add(end);
            }

            positions = next;
        }

        return positions.Contains(target.Length);
    }

    // ü has no ASCII letter; it's written as u, v or yu (lv, lu, lyu for 绿).
    private static IEnumerable<string> WithoutUmlaut(string reading) =>
        reading.Contains('v')
            ? [reading.Replace('v', 'u'), reading, reading.Replace("v", "yu")]
            : [reading];

    private static bool IsHan(Rune rune) =>
        rune.Value
            is >= 0x4E00
                and <= 0x9FFF
                or >= 0x3400
                and <= 0x4DBF
                or >= 0x20000
                and <= 0x3134F
                or >= 0xF900
                and <= 0xFAFF;

    private static Dictionary<string, string[]> LoadReadings()
    {
        using var stream = typeof(ChineseRomaniser).Assembly.GetManifestResourceStream(
            "MapsetVerifier.Checks.pinyin.txt"
        )!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var readings = new Dictionary<string, string[]>();
        while (reader.ReadLine() is { } line)
        {
            var tab = line.IndexOf('\t');
            if (tab > 0)
                readings[line[..tab]] = line[(tab + 1)..].Split(',');
        }

        return readings;
    }

    [GeneratedRegex("[A-Za-z]+")]
    private static partial Regex Word();
}
