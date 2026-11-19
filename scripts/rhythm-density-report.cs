#:project ../MapsetVerifier.Checks/MapsetVerifier.Checks.csproj

// Reports how dense the rhythm of each difficulty is, in 180 BPM beats, grouped by mode and tier.
// Used to pick the thresholds of the note density checks from real (mostly ranked) maps.
//
//   dotnet run scripts/rhythm-density-report.cs
//   dotnet run scripts/rhythm-density-report.cs -- --songs "D:\osu!\Songs" --mode taiko
//   dotnet run scripts/rhythm-density-report.cs -- --csv density.csv
//
// Each column is the share of gaps faster than that snap at 180 BPM, e.g. "<1/2" for a Normal is
// how much of the map is 1/2 or faster. The tier table shows the median, the 90th percentile and the
// highest map, so a threshold can be set just above what ranked maps of that tier do.

using System.Globalization;
using System.Text;
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
var modeFilter = GetOption("--mode");
var csvPath = GetOption("--csv");

if (!Directory.Exists(songsPath))
{
    Console.Error.WriteLine($"Songs folder not found: {songsPath}");
    return 1;
}

double[] divisors = [0.5, 1, 2, 3, 4, 6];
var setPaths = Directory
    .EnumerateDirectories(songsPath)
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToList();
var rows = new List<Row>();
var failures = 0;

foreach (var (setPath, index) in setPaths.Select((path, i) => (path, i)))
{
    Console.Error.Write($"\rReading set {index + 1}/{setPaths.Count}...");

    try
    {
        foreach (var beatmap in new BeatmapSet(setPath).Beatmaps)
        {
            var mode = beatmap.GeneralSettings.mode;

            if (mode == Beatmap.Mode.Mania)
                continue;

            if (
                modeFilter != null
                && !mode.ToString().Equals(modeFilter, StringComparison.OrdinalIgnoreCase)
            )
                continue;

            var gaps = RhythmUtils.GetGaps(beatmap);

            if (gaps.Count < 20)
                continue;

            rows.Add(
                new Row(
                    mode.ToString(),
                    beatmap.GetModeDifficultyName(),
                    (int)beatmap.GetDifficulty(),
                    Path.GetFileName(setPath),
                    beatmap.MetadataSettings.version,
                    gaps.Count,
                    divisors
                        .Select(divisor => RhythmUtils.GetShareFasterThan(gaps, divisor))
                        .ToArray()
                )
            );
        }
    }
    catch (Exception)
    {
        failures++;
    }
}

Console.Error.WriteLine("\r".PadRight(40));
Console.WriteLine($"{rows.Count} difficulties, {failures} sets failed to load");
Console.WriteLine();

var header = string.Concat(divisors.Select(d => $"{"<1/" + d, 18}"));
Console.WriteLine($"{"Mode", -9}{"Tier", -11}{"Maps", 5}{header}");
Console.WriteLine($"{"", -25}{string.Concat(divisors.Select(_ => $"{"p50/p90/max", 18}"))}");
Console.WriteLine(new string('-', 25 + 18 * divisors.Length));

foreach (
    var group in rows.GroupBy(row => (row.Mode, row.Tier))
        .OrderBy(group => group.Key.Mode)
        .ThenBy(group => group.Min(row => row.TierOrder))
)
{
    var cells = divisors.Select(
        (_, i) =>
        {
            var shares = group.Select(row => row.Shares[i]).Order().ToList();
            return $"{$"{Pct(Percentile(shares, 0.5))}/{Pct(Percentile(shares, 0.9))}/{Pct(shares[^1])}", 18}";
        }
    );

    Console.WriteLine(
        $"{group.Key.Mode, -9}{group.Key.Tier, -11}{group.Count(), 5}{string.Concat(cells)}"
    );
}

if (csvPath != null)
{
    var csv = new StringBuilder();
    csv.AppendLine(
        "mode,tier,set,version,gaps," + string.Join(",", divisors.Select(d => $"faster_than_1_{d}"))
    );

    foreach (var row in rows)
        csv.AppendLine(
            $"{row.Mode},{row.Tier},\"{row.Set.Replace("\"", "\"\"")}\",\"{row.Version.Replace("\"", "\"\"")}\",{row.Gaps},"
                + string.Join(",", row.Shares.Select(share => share.ToString("0.0000")))
        );

    File.WriteAllText(csvPath, csv.ToString());
    Console.WriteLine();
    Console.WriteLine($"Wrote {csvPath}");
}

return 0;

static double Percentile(List<double> sorted, double p) =>
    sorted[(int)Math.Round((sorted.Count - 1) * p)];

static string Pct(double share) => $"{share * 100:0}";

string? GetOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

record Row(
    string Mode,
    string Tier,
    int TierOrder,
    string Set,
    string Version,
    int Gaps,
    double[] Shares
);
