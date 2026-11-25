using System.Globalization;
using System.Text;

namespace MapsetVerifier.Checks.Utils.Romanisation;

/// <summary>
///     One piece of a unicode field, with every romanisation the system accepts for it. Units are matched in
///     order against the letters of the romanised field.
/// </summary>
public sealed class RomanisationUnit(char? source, List<string> alternatives)
{
    /// <summary> The character this unit was read from, or null for plain latin letters. </summary>
    public char? Source { get; } = source;

    /// <summary> Accepted romanisations, the preferred one first. May contain an empty string when optional. </summary>
    public List<string> Alternatives { get; set; } = alternatives;

    /// <summary> Whether a new word starts here, so suggestions can put a space before it. </summary>
    public bool StartsWord { get; set; }

    /// <summary> Whether the word starting here is a particle, which suggestions keep lowercase. </summary>
    public bool IsParticle { get; set; }

    /// <summary> Whether the source letter is uppercase, for scripts with letter case. </summary>
    public bool IsUppercase { get; set; }
}

/// <summary> How well a romanised field fits the unicode field. </summary>
public enum RomanisationVerdict
{
    /// <summary> The romaniser can't read the unicode field, so nothing can be said. </summary>
    Unreadable,

    /// <summary> The romanised field follows the expected system. </summary>
    Matches,

    /// <summary> The romanised field doesn't follow the expected system. </summary>
    Mismatch,

    /// <summary>
    ///     The romanised field doesn't follow the expected system, but it may legitimately use different
    ///     words altogether, such as loan words written in their source language.
    /// </summary>
    Inconclusive,
}

/// <summary>
///     Romanises one language as the Ranking Criteria describes it. Each romaniser turns a unicode field into
///     units of accepted romanisations, and this base class takes care of comparing those against the romanised
///     field and suggesting a fix.
/// </summary>
public abstract class Romaniser
{
    /// <summary> Short unique name, also used as the issue template key, e.g. "Japanese". </summary>
    public abstract string Name { get; }

    /// <summary> The Ranking Criteria rule this romaniser enforces. </summary>
    public abstract string[] Rules { get; }

    /// <summary>
    ///     Language tags at least one of which the beatmapset needs for this romaniser to apply, or none when the
    ///     script alone is enough. Han characters are both Chinese and Japanese, so the tag decides there.
    /// </summary>
    public virtual string[] RequiredTags => [];

    /// <summary>
    ///     Whether the script has letter case (Cyrillic, accented latin). Suggestions then keep the case as
    ///     written, instead of capitalising every word.
    /// </summary>
    protected virtual bool HasLetterCase => false;

    /// <summary> Describes which fields are checked, used as the issue template cause. </summary>
    public abstract string Cause { get; }

    /// <summary> Whether the text contains this romaniser's script at all. </summary>
    public bool AppliesTo(string text) => ContainsScript(Normalise(text));

    /// <summary>
    ///     For languages whose rule is about how the romanisation is written rather than spelled (Chinese: each
    ///     character its own capitalised word), the warning shown when <see cref="FollowsFormat" /> fails. It takes
    ///     the field, the romanised text and a suggestion.
    /// </summary>
    public virtual string? FormatMessage => null;

    /// <summary> Whether a romanised field whose letters already match is also written in the expected format. </summary>
    public virtual bool FollowsFormat(string unicode, string romanised) => true;

    /// <summary> Whether this romaniser applies to the text in a beatmapset with these tags. </summary>
    public bool AppliesTo(string text, IReadOnlyCollection<string> tags) =>
        AppliesTo(text)
        && (
            RequiredTags.Length == 0
            || RequiredTags.Any(tag => tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        );

    /// <summary> Whether the already normalised text contains this romaniser's script. </summary>
    protected abstract bool ContainsScript(string text);

    /// <summary>
    ///     Reads the text into units. Separators like spaces and punctuation are left out, since only letters are
    ///     compared. Returns null when the text contains anything this romaniser can't read.
    /// </summary>
    protected abstract List<RomanisationUnit>? Read(string text);

    /// <summary> Same as <see cref="Read" />, for a part of the text, which the whole may tell the language of. </summary>
    protected virtual List<RomanisationUnit>? ReadPart(string part, string text) => Read(part);

    /// <summary>
    ///     Like <see cref="Read" />, but also accepting other common romanisation systems for the language. Used
    ///     to tell a romanisation in the wrong system apart from different words altogether. Null when the
    ///     language has no such systems.
    /// </summary>
    protected virtual List<RomanisationUnit>? ReadAnySystem(string text) => null;

    /// <summary>
    ///     Whether a mismatch could mean the romanised field uses different words, rather than a different
    ///     romanisation. Such mismatches are only reported when another romanisation system fits instead.
    /// </summary>
    protected virtual bool MayUseDifferentWords(string text) => false;

    /// <summary> Whether the letters are another accepted romanisation of the already normalised text. </summary>
    protected virtual bool MatchesOtherwise(string text, string target) => false;

    /// <summary>
    ///     The romanised field with as few letters as possible changed so that it matches the unicode field, or null if
    ///     there's no such correction.
    /// </summary>
    public virtual RomanisationCorrection? Correct(string unicode, string romanised)
    {
        var text = Normalise(unicode);
        var units = Read(text);
        var words = ReadWords(text);
        if (units == null || words == null)
            return null;

        var swap = RomanisationCorrection
            .Swaps(romanised)
            .FirstOrDefault(swap => Matches(units, Letters(swap.Text)));
        if (swap != null)
            return swap;

        var graph = words
            .Select((word, index) => new[] { new RomanisationWord(index + 1, word) })
            .ToList();
        return RomanisationSearch.FindClosest(graph, Letters(romanised)) is { } closest
            ? RomanisationCorrection.Apply(romanised, closest)
            : null;
    }

    /// <summary> Same as <see cref="Read" />, but with the units of each word by themselves. </summary>
    protected virtual List<List<RomanisationUnit>>? ReadWords(string text) => ReadWords(text, Read);

    protected static List<List<RomanisationUnit>>? ReadWords(
        string text,
        Func<string, List<RomanisationUnit>?> read
    )
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(read)
            .ToList();

        return words.Any(word => word == null)
            ? null
            : words.OfType<List<RomanisationUnit>>().ToList();
    }

    /// <summary>
    ///     Whether a field whose romanisation is inconclusive should still be pointed out, so that mistakes in it
    ///     aren't missed, even though it may well be an official spelling.
    /// </summary>
    public virtual bool PointsOutInconclusive(string unicode, IReadOnlyCollection<string> tags) =>
        false;

    public RomanisationVerdict Judge(string unicode, string romanised)
    {
        var text = Normalise(unicode);
        var units = Read(text);
        if (units == null || !units.Any(unit => unit.Source != null))
            return RomanisationVerdict.Unreadable;

        var target = Letters(romanised);
        if (Matches(units, target))
            return RomanisationVerdict.Matches;

        if (!MayUseDifferentWords(text))
            return RomanisationVerdict.Mismatch;

        // Before other spellings, since name lists also have names written in other systems.
        var anySystem = ReadAnySystem(text);
        if (anySystem != null && Matches(anySystem, target))
            return RomanisationVerdict.Mismatch;

        return MatchesOtherwise(text, target)
            ? RomanisationVerdict.Matches
            : RomanisationVerdict.Inconclusive;
    }

    /// <summary>
    ///     The unicode field with each run of script replaced by its preferred romanisation. Latin text and ASCII
    ///     punctuation are kept as written, other symbols (♥, ・, 、) become word breaks, and a romanised run is
    ///     capitalised when it starts a word.
    /// </summary>
    public virtual string Suggest(string unicode)
    {
        var normalised = Normalise(unicode);
        var result = new StringBuilder();
        var run = new StringBuilder();
        var runIsLatin = false;
        // Whether the text so far ends in a latin or a romanised run, null after anything else.
        bool? lastRunWasLatin = null;

        foreach (var ch in normalised)
        {
            // With letter case, plain letters belong to the same word as the script's letters (Für → Fuer).
            var isLatin = ch < 128 && char.IsLetter(ch) && !HasLetterCase;
            if (char.IsLetter(ch))
            {
                if (run.Length > 0 && runIsLatin != isLatin)
                    FlushRun();
                runIsLatin = isLatin;
                run.Append(ch);
                continue;
            }

            FlushRun();
            result.Append(ch < 128 ? ch : ' ');
            lastRunWasLatin = null;
        }

        FlushRun();
        return string.Join(
            " ",
            result.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );

        void FlushRun()
        {
            if (run.Length == 0)
                return;

            var text = run.ToString();
            run.Clear();

            var piece = runIsLatin ? text : RomaniseRun(text, normalised);
            if (piece.Length == 0)
                return;

            // Keep latin and romanised runs that touched in the unicode field apart as separate words.
            if (lastRunWasLatin != null && lastRunWasLatin != runIsLatin)
                result.Append(' ');

            // Stylised joins like "Ha~tobi~to" and "Ko-ji" carry on the same word.
            var startsWord =
                result.Length == 0
                || !(char.IsLetterOrDigit(result[^1]) || result[^1] is '~' or '-' or '\'' or '.');
            if (!runIsLatin && startsWord && !HasLetterCase)
                piece = Capitalise(piece);

            result.Append(piece);
            lastRunWasLatin = runIsLatin;
        }
    }

    /// <summary> The preferred romanisation, with a space before each word and every word but particles capitalised. </summary>
    private string RomaniseRun(string run, string text)
    {
        var units = ReadPart(run, text);
        if (units == null)
            return "";

        // A word written in capitals stays in capitals ("МОСКВА" → "MOSKVA").
        var lettered = units.Where(unit => unit.Alternatives[0].Length > 0).ToList();
        var allCaps = HasLetterCase && lettered.Count > 1 && lettered.All(unit => unit.IsUppercase);

        var romanised = new StringBuilder();
        foreach (var unit in units)
        {
            var alternative = unit.Alternatives[0];
            if (unit.StartsWord && romanised.Length > 0 && alternative.Length > 0)
            {
                romanised.Append(' ');
                if (!unit.IsParticle && !HasLetterCase)
                    alternative = Capitalise(alternative);
            }

            if (allCaps)
                alternative = alternative.ToUpperInvariant();
            else if (unit.IsUppercase)
                alternative = Capitalise(alternative);

            romanised.Append(alternative);
        }

        return romanised.ToString();
    }

    private static string Capitalise(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], CultureInfo.InvariantCulture) + text[1..];

    /// <summary> Folds full-width latin and half-width kana into their regular forms. </summary>
    protected static string Normalise(string text) => text.Normalize(NormalizationForm.FormKC);

    /// <summary> Lowercase latin letters only, since spacing, punctuation and capitalisation aren't compared. </summary>
    protected static string Letters(string text) =>
        new(text.ToLowerInvariant().Where(ch => ch is >= 'a' and <= 'z').ToArray());

    /// <summary> Whether the letters can be built from the units in order, picking one alternative per unit. </summary>
    protected static bool Matches(List<RomanisationUnit> units, string target) =>
        EndsOf(units, target, 0).Contains(target.Length);

    /// <summary> Where in the letters the units may end, picking one alternative per unit from the given start on. </summary>
    internal static HashSet<int> EndsOf(List<RomanisationUnit> units, string target, int start)
    {
        var positions = new HashSet<int> { start };
        foreach (var unit in units)
        {
            var next = new HashSet<int>();
            foreach (var position in positions)
            foreach (var alternative in unit.Alternatives)
                if (target.AsSpan(position).StartsWith(alternative))
                    next.Add(position + alternative.Length);

            positions = next;
            if (positions.Count == 0)
                break;
        }

        return positions;
    }
}
