#:project ../MapsetVerifier.Checks/MapsetVerifier.Checks.csproj

// Measures the note patterns of osu!mania difficulties, grouped by level, to check the limits of the
// mania pattern checks against real (mostly ranked) maps.
//
//   dotnet run scripts/mania-pattern-report.cs
//   dotnet run scripts/mania-pattern-report.cs -- --songs "D:\osu!\Songs" --top anchor4
//
// Columns are the longest run (in rows or notes) or the number of occurrences in a difficulty; the
// table shows the median, the 90th percentile and the highest difficulty of each level.

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

(string Name, Func<Beatmap, double> Measure)[] columns =
[
    ("run1/2", b => MaxRows(b, 2)),
    ("run1/4", b => MaxRows(b, 4)),
    ("run1/6", b => MaxRows(b, 6)),
    ("run1/8", b => MaxRows(b, 8)),
    ("chord1/2", b => ManiaPatterns.ChordsInsideRuns(b, 2).Count()),
    ("chord1/4", b => ManiaPatterns.ChordsInsideRuns(b, 4).Count()),
    ("chord1/6", b => ManiaPatterns.ChordsInsideRuns(b, 6).Count()),
    ("anchor2", b => ManiaPatterns.Anchors(b, 2).Select(r => r.Count).DefaultIfEmpty(0).Max()),
    ("anchor4", b => ManiaPatterns.Anchors(b, 4).Select(r => r.Count).DefaultIfEmpty(0).Max()),
    ("jack1/4", b => ManiaPatterns.Jacks(b, 4).Count()),
    ("trill1/4", b => ManiaPatterns.Trills(b, 4).Select(r => r.Count).DefaultIfEmpty(0).Max()),
    ("ln<1", b => ManiaPatterns.ShortLongNotes(b, 1).Count()),
    ("ln<1/2", b => ManiaPatterns.ShortLongNotes(b, 0.5).Count()),
    ("ln<1/4", b => ManiaPatterns.ShortLongNotes(b, 0.25).Count()),
    ("rel<1", b => ManiaPatterns.CloseReleases(b, 1).Count()),
    ("rel<1/2", b => ManiaPatterns.CloseReleases(b, 0.5).Count()),
    ("adjrel", b => ManiaPatterns.AdjacentReleaseMismatches(b).Count()),
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
        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania || beatmap.IsDifficultyGuessed())
            continue;

        rows.Add(
            (
                beatmap.GetModeDifficultyName(),
                (int)beatmap.GetDifficulty(),
                $"{Path.GetFileName(setPath)} [{beatmap.MetadataSettings.version}]",
                columns.Select(column => column.Measure(beatmap)).ToArray()
            )
        );
    }
}

Console.WriteLine($"{rows.Count} osu!mania difficulties with a named level");

foreach (var chunk in columns.Select((c, i) => (c, i)).Chunk(6))
{
    Console.WriteLine();
    Console.WriteLine(
        $"{"Tier", -8}{"Maps", 5}" + string.Concat(chunk.Select(c => $"{c.c.Name, 14}"))
    );

    foreach (
        var group in rows.GroupBy(row => row.Tier).OrderBy(group => group.Min(row => row.Order))
    )
    {
        var cells = chunk.Select(c =>
        {
            var values = group.Select(row => row.Values[c.i]).Order().ToList();
            return $"{$"{At(values, 0.5)}/{At(values, 0.9)}/{values[^1]}", 14}";
        });

        Console.WriteLine($"{group.Key, -8}{group.Count(), 5}{string.Concat(cells)}");
    }
}

if (top != null && Array.FindIndex(columns, c => c.Name == top) is var index and >= 0)
{
    foreach (
        var group in rows.GroupBy(row => row.Tier).OrderBy(group => group.Min(row => row.Order))
    )
    {
        Console.WriteLine();
        Console.WriteLine($"Highest {top} in {group.Key}");

        foreach (var row in group.OrderByDescending(row => row.Values[index]).Take(5))
            Console.WriteLine($"  {row.Values[index], 6}  {row.Name}");
    }
}

return 0;

static double MaxRows(Beatmap beatmap, double divisor) =>
    ManiaPatterns.RowRuns(beatmap, divisor).Select(run => run.Count).DefaultIfEmpty(0).Max();

static double At(List<double> sorted, double p) => sorted[(int)Math.Round((sorted.Count - 1) * p)];

string? GetOption(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
