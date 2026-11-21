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

    /// <summary> The system as shown to users, e.g. "Modified Hepburn". </summary>
    public abstract string SystemName { get; }

    /// <summary> The Ranking Criteria rule this romaniser enforces. </summary>
    public abstract string Rule { get; }

    /// <summary> Describes which fields are checked, used as the issue template cause. </summary>
    public abstract string Cause { get; }

    /// <summary> Whether the text contains this romaniser's script at all. </summary>
    public bool AppliesTo(string text) => ContainsScript(Normalise(text));

    /// <summary> Whether the already normalised text contains this romaniser's script. </summary>
    protected abstract bool ContainsScript(string text);

    /// <summary>
    ///     Reads the text into units. Separators like spaces and punctuation are left out, since only letters are
    ///     compared. Returns null when the text contains anything this romaniser can't read.
    /// </summary>
    protected abstract List<RomanisationUnit>? Read(string text);

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

        var anySystem = ReadAnySystem(text);
        return anySystem != null && Matches(anySystem, target)
            ? RomanisationVerdict.Mismatch
            : RomanisationVerdict.Inconclusive;
    }

    /// <summary> The preferred romanisation of each word, keeping the spacing and capitalising each word. </summary>
    public virtual string Suggest(string unicode)
    {
        var words = Normalise(unicode)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word =>
            {
                var units = Read(word);
                if (units == null || !units.Any(unit => unit.Source != null))
                    return new string(word.Where(ch => ch < 128).ToArray());

                var romaji = string.Concat(units.Select(unit => unit.Alternatives[0]));
                return romaji.Length == 0
                    ? romaji
                    : char.ToUpper(romaji[0], CultureInfo.InvariantCulture) + romaji[1..];
            })
            .Where(word => word.Length > 0);

        return string.Join(" ", words);
    }

    /// <summary> Folds full-width latin and half-width kana into their regular forms. </summary>
    protected static string Normalise(string text) => text.Normalize(NormalizationForm.FormKC);

    /// <summary> Lowercase latin letters only, since spacing, punctuation and capitalisation aren't compared. </summary>
    protected static string Letters(string text) =>
        new(text.ToLowerInvariant().Where(ch => ch is >= 'a' and <= 'z').ToArray());

    /// <summary> Whether the letters can be built from the units in order, picking one alternative per unit. </summary>
    protected static bool Matches(List<RomanisationUnit> units, string target)
    {
        var positions = new HashSet<int> { 0 };
        foreach (var unit in units)
        {
            var next = new HashSet<int>();
            foreach (var position in positions)
            foreach (var alternative in unit.Alternatives)
                if (target.AsSpan(position).StartsWith(alternative))
                    next.Add(position + alternative.Length);

            if (next.Count == 0)
                return false;
            positions = next;
        }

        return positions.Contains(target.Length);
    }
}
