using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Settings;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Mania;

/// <summary>
///     The key mode or playstyle a mania difficulty is played in: "7K", "7K+1" for the N+1 style, or
///     one of the playstyles above 10K, such as "DP". The key count comes from the file; the N+1 style
///     and a playstyle above 10K come from the name, when they fit the key count. The file's special
///     style option is left out: it's easily left on in one difficulty of a set named 8K throughout.
///     Keys is the key count, to put them in order.
///     <para>
///         A set with more than one has every difficulty's name denote it. <see cref="Marker" /> is the
///         key mode or playstyle the name has, as written ("[7K]"), whether it's this one or not
///         (<see cref="Denoted" />), and <see cref="ShortName" /> the name without it and the divider
///         next to it ("Mapper's Insane" for "Mapper's [7K] - Insane"); both null when the name has
///         none, and the short name when the name is nothing but the marker.
///     </para>
/// </summary>
public sealed record ManiaPlaystyle(
    string Label,
    int Keys,
    string? Marker,
    string? ShortName,
    bool Denoted
)
{
    /// <summary> The playstyles above 10K, their other names, and the key counts each is played with. </summary>
    private static readonly (string Name, string[] Aliases, int[] Keys)[] Named =
    [
        ("10K2S", [], [12]),
        ("DP", [], [14, 16]),
        ("EZ2AC", [], [14, 16]),
        ("10K8K", ["8K10K", "4K10K4K"], [18]),
        ("9K9K", [], [18]),
    ];

    /// <summary>
    ///     Any way of writing a key count: "7K", "7k", "7 Keys", "7Key", "7-key", "14K2S" (keys and
    ///     scratches), and for N+1 "7K+1", "7+1K" or "7+1".
    /// </summary>
    private const string AnyKeys = @"\d{1,2}(?:K(?:\d{1,2}S|\+1)?|\+1K?|[\s\-]?Keys?)";

    /// <summary> Any key mode or playstyle, for a name that has one that isn't its own. </summary>
    private static readonly string AnyMarker = string.Join(
        "|",
        Named.SelectMany(n => n.Aliases.Prepend(n.Name)).Select(NamedPattern).Append(AnyKeys)
    );

    /// <summary> What divides a marker from the rest of the name, as in "7K - Hard". </summary>
    private const string Divider = @"[\s\-–—:|~/]*";

    public static ManiaPlaystyle? Of(Beatmap beatmap) =>
        Of(
            beatmap.GeneralSettings.mode,
            beatmap.DifficultySettings.circleSize,
            beatmap.MetadataSettings.version
        );

    /// <summary> From a difficulty's code, without parsing its objects, as for a stored version. </summary>
    public static ManiaPlaystyle? Of(string code, string version)
    {
        var lines = code.Split(["\n"], StringSplitOptions.None);
        var general = ParserStatic.GetSettings(lines, "General", l => new GeneralSettings(l));
        if (general.mode != Beatmap.Mode.Mania)
            return null;

        var difficulty = ParserStatic.GetSettings(
            lines,
            "Difficulty",
            l => new DifficultySettings(l)
        );
        return Of(general.mode, difficulty.circleSize, version);
    }

    public static ManiaPlaystyle? Of(Beatmap.Mode mode, float circleSize, string version)
    {
        if (mode != Beatmap.Mode.Mania)
            return null;

        var keys = Math.Max(1, (int)circleSize);

        foreach (var (name, aliases, counts) in Named)
        {
            if (
                counts.Contains(keys)
                && FindMarker(version, aliases.Prepend(name).Select(NamedPattern)) is { } named
            )
                return new ManiaPlaystyle(name, keys, named.Marker, named.Without, true);
        }

        if (keys > 1 && FindMarker(version, N1Keys(keys)) is { } n1)
            return new ManiaPlaystyle($"{keys - 1}K+1", keys, n1.Marker, n1.Without, true);

        var found = FindMarker(version, OwnKeys(keys));
        var denoted = found != null;
        found ??= FindMarker(version, [AnyMarker]);

        return new ManiaPlaystyle($"{keys}K", keys, found?.Marker, found?.Without, denoted);
    }

    /// <summary>
    ///     A playstyle by name, with the key count it's often written with next to it, as in "14K EZ2AC"
    ///     or "DP 14K2S".
    /// </summary>
    private static string NamedPattern(string name) =>
        $@"(?:{AnyKeys}[\s\-]+)?{Regex.Escape(name)}(?:[\s\-]+{AnyKeys})?";

    /// <summary> The ways of writing the N+1 style of this key count: "8K (7+1)", "7K+1", "7+1K", "7+1". </summary>
    private static IEnumerable<string> N1Keys(int keys)
    {
        var n = keys - 1;
        yield return $@"{keys}K\s*\({n}\+1\)";
        yield return $@"{n}K\+1|{n}\+1K?";
    }

    /// <summary> The ways of writing this key count ("8K", "8 Keys"), and keys and scratches ("14K2S" for 16K). </summary>
    private static IEnumerable<string> OwnKeys(int keys)
    {
        yield return $@"{keys}(?:K|[\s\-]?Keys?)";

        if (keys > 2)
            yield return $"{keys - 2}K2S";
    }

    /// <summary>
    ///     The first of the marker patterns in the name, bare or in brackets, anywhere in it ("[7K] Hard",
    ///     "Hard (7K)", "Mapper's 7K Insane"), and the name without it and its divider. A marker is a whole
    ///     word, so "7K" isn't found in "17K" or "7K+1".
    /// </summary>
    private static (string Marker, string Without)? FindMarker(
        string version,
        IEnumerable<string> patterns
    )
    {
        foreach (var pattern in patterns)
        {
            var m = $"(?:{pattern})";
            var match = Regex.Match(
                version,
                $@"{Divider}(?<![\p{{L}}\p{{N}}+])(?<marker>\[{m}\]|\({m}\)|{m})(?![\p{{L}}\p{{N}}+]){Divider}",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                continue;

            var rest = version.Remove(match.Index, match.Length).Insert(match.Index, " ").Trim();
            return (match.Groups["marker"].Value, rest.Length > 0 ? rest : version);
        }

        return null;
    }
}
