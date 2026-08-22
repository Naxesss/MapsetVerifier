#:project ../MapsetVerifier.Parser/MapsetVerifier.Parser.csproj

// Reports how osu!catch movements classify as walks, dashes and hyperdashes, grouped by difficulty.
//
//   dotnet run scripts/catch-movement-report.cs
//   dotnet run scripts/catch-movement-report.cs -- --songs "D:\osu!\Songs" --set "Eternity"
//   dotnet run scripts/catch-movement-report.cs -- --csv report.csv
//
// Expect roughly: Cups have no dashes or hyperdashes at all, Salads introduce dashes but still have
// no hyperdashes, and both grow from Platter upwards.

using System.Globalization;
using System.Text;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

// Keep the table readable regardless of the machine's regional settings.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var songsPath =
    GetOption("--songs")
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "osu!",
        "Songs"
    );
var setFilter = GetOption("--set");
var csvPath = GetOption("--csv");

if (!Directory.Exists(songsPath))
{
    Console.Error.WriteLine($"Songs folder not found: {songsPath}");
    Console.Error.WriteLine("Pass one explicitly with --songs \"<path>\".");
    return 1;
}

var setPaths = Directory
    .EnumerateDirectories(songsPath)
    .Where(path =>
        setFilter == null
        || Path.GetFileName(path).Contains(setFilter, StringComparison.OrdinalIgnoreCase)
    )
    .Where(ContainsCatchDifficulty)
    .Order(StringComparer.OrdinalIgnoreCase)
    .ToList();

Console.WriteLine($"Songs folder : {songsPath}");
Console.WriteLine($"Catch sets   : {setPaths.Count}");
Console.WriteLine();

var rows = new List<Row>();
var failures = new List<string>();

foreach (var (setPath, index) in setPaths.Select((path, i) => (path, i)))
{
    Console.Write($"\rReading set {index + 1}/{setPaths.Count}...");

    try
    {
        foreach (var beatmap in new BeatmapSet(setPath).Beatmaps)
        {
            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Catch)
                continue;

            var objects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

            rows.Add(
                new Row(
                    Tier: beatmap.GetModeDifficultyName(),
                    TierOrder: (int)beatmap.GetDifficulty(),
                    SetName: Path.GetFileName(setPath),
                    Version: beatmap.MetadataSettings.version,
                    CircleSize: beatmap.DifficultySettings.circleSize,
                    Walks: objects.Count(o => o.MovementType == CatchMovementType.Walk),
                    Dashes: objects.Count(o => o.MovementType == CatchMovementType.Dash),
                    Hypers: objects.Count(o => o.MovementType == CatchMovementType.Hyperdash)
                )
            );
        }
    }
    catch (Exception exception)
    {
        failures.Add($"{Path.GetFileName(setPath)}: {exception.Message}");
    }
}

Console.WriteLine("\r".PadRight(40));

if (rows.Count == 0)
{
    Console.Error.WriteLine("No catch difficulties found.");
    return 1;
}

// Expert and Ultra both report as Overdose, so group on the name rather than the enum.
var tiers = rows.GroupBy(row => row.Tier)
    .Select(group => new
    {
        Tier = group.Key,
        Order = group.Min(row => row.TierOrder),
        Maps = group.Count(),
        Walks = group.Sum(row => (long)row.Walks),
        Dashes = group.Sum(row => (long)row.Dashes),
        Hypers = group.Sum(row => (long)row.Hypers),
    })
    .OrderBy(tier => tier.Order)
    .ToList();

Console.WriteLine("Per difficulty");
Console.WriteLine(
    $"{"Tier", -9} {"Maps", 4} {"Movements", 10} {"Walk", 9} {"Dash", 8} {"Hyper", 8} {"Dash%", 7} {"Hyper%", 7}"
);
Console.WriteLine(new string('-', 70));

foreach (var tier in tiers)
{
    var total = tier.Walks + tier.Dashes + tier.Hypers;
    Console.WriteLine(
        $"{tier.Tier, -9} {tier.Maps, 4} {total, 10:N0} {tier.Walks, 9:N0} {tier.Dashes, 8:N0} "
            + $"{tier.Hypers, 8:N0} {Percent(tier.Dashes, total), 7} {Percent(tier.Hypers, total), 7}"
    );
}

var grandTotal = rows.Sum(row => (long)(row.Walks + row.Dashes + row.Hypers));
Console.WriteLine(new string('-', 70));
Console.WriteLine(
    $"{"All", -9} {rows.Count, 4} {grandTotal, 10:N0} {rows.Sum(r => (long)r.Walks), 9:N0} "
        + $"{rows.Sum(r => (long)r.Dashes), 8:N0} {rows.Sum(r => (long)r.Hypers), 8:N0}"
);

Console.WriteLine();
Console.WriteLine("Per beatmap");
Console.WriteLine($"{"Tier", -9} {"CS", 4} {"Walk", 7} {"Dash", 6} {"Hyper", 6}  Difficulty");
Console.WriteLine(new string('-', 90));

foreach (var row in rows.OrderBy(r => r.TierOrder).ThenBy(r => r.SetName).ThenBy(r => r.Version))
{
    Console.WriteLine(
        $"{row.Tier, -9} {row.CircleSize, 4:0.#} {row.Walks, 7:N0} {row.Dashes, 6:N0} {row.Hypers, 6:N0}"
            + $"  [{row.Version}] {row.SetName}"
    );
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Skipped {failures.Count} set(s) that failed to parse:");
    foreach (var failure in failures)
        Console.WriteLine($"  {failure}");
}

if (csvPath != null)
{
    var csv = new StringBuilder("tier,circleSize,walks,dashes,hypers,version,set\n");
    foreach (var row in rows.OrderBy(r => r.TierOrder).ThenBy(r => r.SetName))
        csv.AppendLine(
            string.Join(
                ',',
                row.Tier,
                row.CircleSize.ToString(CultureInfo.InvariantCulture),
                row.Walks,
                row.Dashes,
                row.Hypers,
                Quote(row.Version),
                Quote(row.SetName)
            )
        );

    File.WriteAllText(csvPath, csv.ToString());
    Console.WriteLine();
    Console.WriteLine($"Wrote {Path.GetFullPath(csvPath)}");
}

return 0;

string? GetOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string Percent(long part, long total) =>
    total == 0 ? "-" : (100.0 * part / total).ToString("0.0", CultureInfo.InvariantCulture) + "%";

static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

// Reading the mode straight from the file avoids fully parsing the hundreds of sets that are not catch.
static bool ContainsCatchDifficulty(string setPath)
{
    foreach (var file in Directory.EnumerateFiles(setPath, "*.osu"))
    {
        try
        {
            foreach (var line in File.ReadLines(file))
            {
                if (line.StartsWith("[Metadata]", StringComparison.Ordinal))
                    break;

                if (
                    line.StartsWith("Mode:", StringComparison.Ordinal)
                    && line["Mode:".Length..].Trim() == "2"
                )
                    return true;
            }
        }
        catch (IOException)
        {
            // Unreadable file, just try the next one.
        }
    }

    return false;
}

record Row(
    string Tier,
    int TierOrder,
    string SetName,
    string Version,
    float CircleSize,
    int Walks,
    int Dashes,
    int Hypers
);
