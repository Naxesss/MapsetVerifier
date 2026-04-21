using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;

namespace MapsetVerifier.Exports;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            Checker.SuppressLoadLogging = true;
            Checker.LoadDefaultChecks();

            var checks = CheckerRegistry.GetChecks();
            var items = checks.Select(SerializeCheck).ToList();

            var root = new
            {
                generatedAt = DateTime.UtcNow.ToString("o"),
                version = GetVersion(),
                checks = items,
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            };

            var json = JsonSerializer.Serialize(root, options);
            var outputPath =
                args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                    ? args[0]
                    : Path.Combine(FindRepoRoot(), "scripts", "checks-metadata.json");

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(outputPath, json);
            Console.Error.WriteLine($"Written {items.Count} checks to {outputPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Finds the repository root by walking up until MapsetVerifier.sln or .git is found.</summary>
    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        if (string.IsNullOrEmpty(dir))
            dir = Directory.GetCurrentDirectory();

        for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "MapsetVerifier.sln")) ||
                Directory.Exists(Path.Combine(d.FullName, ".git")))
                return d.FullName;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string GetVersion()
    {
        try
        {
            var asm = Assembly.GetEntryAssembly();
            return asm?.GetName().Version?.ToString() ?? "0.0.0";
        }
        catch
        {
            return "0.0.0";
        }
    }

    private static object SerializeCheck(Check check)
    {
        var meta = check.GetMetadata();
        var checkType = check switch
        {
            BeatmapCheck => "Beatmap",
            BeatmapSetCheck => "BeatmapSet",
            GeneralCheck => "General",
            _ => "Unknown",
        };

        var item = new Dictionary<string, object?>
        {
            ["name"] = check.GetType().Name,
            ["fullName"] = check.GetType().FullName,
            ["checkType"] = checkType,
            ["category"] = meta.Category,
            ["message"] = meta.Message,
            ["author"] = meta.Author,
            ["applicableModes"] = meta.GetMode(),
            ["documentation"] = meta.Documentation,
            ["templates"] = check
                .GetTemplates()
                .ToDictionary(
                    kv => kv.Key,
                    kv => new
                    {
                        level = kv.Value.Level.ToString(),
                        formatString = kv.Value.FormatString,
                        defaultArguments = kv
                            .Value.GetDefaultArguments()
                            .Select(a => a.ToString())
                            .ToArray(),
                        cause = kv.Value.Cause,
                    }
                ),
        };

        if (meta is BeatmapCheckMetadata bmMeta)
        {
            item["modes"] = bmMeta.Modes.Select(m => m.ToString()).ToArray();
            item["difficulties"] = bmMeta.Difficulties.Select(d => d.ToString()).ToArray();
        }

        return item;
    }
}
