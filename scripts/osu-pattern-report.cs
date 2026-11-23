#:project ../MapsetVerifier.Checks/MapsetVerifier.Checks.csproj

// Measures rhythm and structure patterns of osu! difficulties, grouped by level, to pick the limits
// of the pattern checks from real (mostly ranked) maps. Gaps are measured in 180 BPM beats.
//
//   dotnet run scripts/osu-pattern-report.cs
//   dotnet run scripts/osu-pattern-report.cs -- --songs "D:\osu!\Songs" --top clicked
//
// Columns are the longest run of each pattern in a difficulty, or for "bodies" and "ends" the number of
// fully overlapped slider bodies and of overlapping circles, heads and tails (Easy and Normal only). The
// table shows the median, the 90th percentile and the highest difficulty of each level. --top <column>
// lists the difficulties with the highest values for one column.

using System.Globalization;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Parser.Objects;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var songsPath =
    GetOption("--songs")
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "osu!",
        "Songs"
    );
var top = GetOption("--top");

string[] columns =
[
    "clicked",
    "sliders",
    "chain",
    "norest",
    "stream",
    "combo",
    "svmin",
    "bodies",
    "ends",
];
var rows = new List<(string Tier, int Order, string Name, double[] Values)>();

foreach (var setPath in Directory.EnumerateDirectories(songsPath))
{
    BeatmapSet set;

    try
    {
        set = new BeatmapSet(setPath);
    }
    catch (Exception)
    {
        continue;
    }

    foreach (var beatmap in set.Beatmaps)
    {
        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Standard || beatmap.IsDifficultyGuessed())
            continue;

        var patterns = OsuPatterns.Measure(beatmap);

        rows.Add(
            (
                beatmap.GetModeDifficultyName(),
                (int)beatmap.GetDifficulty(),
                $"{Path.GetFileName(setPath)} [{beatmap.MetadataSettings.version}]",
                [
                    patterns.ClickedHalfRun,
                    patterns.HalfSliderRun,
                    patterns.HalfGapChain,
                    patterns.LongestWithoutRest,
                    patterns.LongestStream,
                    patterns.LongestCombo,
                    patterns.SvChangesPerMinute,
                    CountBodyOverlaps(beatmap),
                    beatmap.GetDifficulty() <= Beatmap.Difficulty.Normal
                        ? OsuOverlaps.EndOverlaps(beatmap).Count()
                        : 0,
                ]
            )
        );
    }
}

Console.WriteLine($"{rows.Count} osu! difficulties with a named level");
Console.WriteLine();
Console.WriteLine($"{"Tier", -8}{"Maps", 5}" + string.Concat(columns.Select(c => $"{c, 16}")));
Console.WriteLine(new string('-', 13 + 16 * columns.Length));

foreach (var group in rows.GroupBy(row => row.Tier).OrderBy(group => group.Min(row => row.Order)))
{
    var cells = columns.Select(
        (_, i) =>
        {
            var values = group.Select(row => row.Values[i]).Order().ToList();
            return $"{$"{Fmt(At(values, 0.5))}/{Fmt(At(values, 0.9))}/{Fmt(values[^1])}", 16}";
        }
    );

    Console.WriteLine($"{group.Key, -8}{group.Count(), 5}{string.Concat(cells)}");
}

if (top != null && Array.IndexOf(columns, top) is var index and >= 0)
{
    foreach (
        var group in rows.GroupBy(row => row.Tier).OrderBy(group => group.Min(row => row.Order))
    )
    {
        Console.WriteLine();
        Console.WriteLine($"Highest {top} in {group.Key}");

        foreach (var row in group.OrderByDescending(row => row.Values[index]).Take(6))
            Console.WriteLine($"  {Fmt(row.Values[index]), 6}  {row.Name}");
    }
}

return 0;

static int CountBodyOverlaps(Beatmap beatmap)
{
    double? beats = beatmap.GetDifficulty() switch
    {
        Beatmap.Difficulty.Easy => 4,
        Beatmap.Difficulty.Normal => 3,
        Beatmap.Difficulty.Hard => 2,
        Beatmap.Difficulty.Insane => 0.5,
        _ => null,
    };

    return beats is { } limit ? OsuOverlaps.BodyOverlaps(beatmap, limit).Count() : 0;
}

static double At(List<double> sorted, double p) => sorted[(int)Math.Round((sorted.Count - 1) * p)];

static string Fmt(double value) => value.ToString(value % 1 == 0 ? "0" : "0.0");

string? GetOption(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
