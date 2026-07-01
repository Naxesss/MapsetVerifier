using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using Serilog;

namespace MapsetVerifier.Server.Service;

public static class CheckRunHistoryService
{
    private const string ExternalsFolderName = "Mapset Verifier Externals";
    private const string CheckRunsFolderName = "check-runs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static ApiCheckRunDelta? BuildDeltaAndRememberCurrent(
        BeatmapSet beatmapSet,
        ApiBeatmapSetCheckResult currentResult
    )
    {
        try
        {
            var currentSignature = BuildFolderSignature(beatmapSet);
            var currentRun = StoredCheckRun.FromResult(
                DateTime.UtcNow,
                currentSignature,
                currentResult
            );
            var path = GetHistoryPath(beatmapSet);
            var previousRun = ReadPreviousRun(path);

            ApiCheckRunDelta? delta = null;

            if (previousRun != null && previousRun.FolderSignature != currentSignature)
                delta = BuildDelta(previousRun, currentRun);

            WriteCurrentRun(path, currentRun);

            return delta;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to build check run delta for {SongPath}", beatmapSet.SongPath);
            return null;
        }
    }

    private static ApiCheckRunDelta BuildDelta(
        StoredCheckRun previousRun,
        StoredCheckRun currentRun
    )
    {
        var previousByKey = ToIssueDictionary(previousRun.Issues);
        var currentByKey = ToIssueDictionary(currentRun.Issues);

        var newIssues = currentRun
            .Issues.Where(issue => !previousByKey.ContainsKey(issue.Key))
            .Select(issue => issue.ToApiIssue(null))
            .ToList();

        var resolvedIssues = previousRun
            .Issues.Where(issue => !currentByKey.ContainsKey(issue.Key))
            .Select(issue => issue.ToApiIssue(null))
            .ToList();

        var sharedIssues = currentRun
            .Issues.Where(issue => previousByKey.ContainsKey(issue.Key))
            .ToList();

        var worsenedIssues = sharedIssues
            .Where(issue => IsWorse(issue.Level, previousByKey[issue.Key].Level))
            .Select(issue => issue.ToApiIssue(previousByKey[issue.Key].Level))
            .ToList();

        var unchangedIssues = sharedIssues
            .Where(issue => !IsWorse(issue.Level, previousByKey[issue.Key].Level))
            .Select(issue => issue.ToApiIssue(previousByKey[issue.Key].Level))
            .ToList();

        return new ApiCheckRunDelta(
            previousRun.RunAt,
            currentRun.RunAt,
            newIssues,
            resolvedIssues,
            worsenedIssues,
            unchangedIssues
        );
    }

    private static Dictionary<string, StoredCheckIssue> ToIssueDictionary(
        IEnumerable<StoredCheckIssue> issues
    ) =>
        issues.GroupBy(issue => issue.Key).ToDictionary(group => group.Key, group => group.First());

    private static bool IsWorse(Issue.Level current, Issue.Level previous) =>
        GetSeverityRank(current) > GetSeverityRank(previous);

    private static int GetSeverityRank(Issue.Level level) =>
        level switch
        {
            Issue.Level.Info => 0,
            Issue.Level.Check => 0,
            Issue.Level.Minor => 1,
            Issue.Level.Warning => 2,
            Issue.Level.Problem => 3,
            Issue.Level.Error => 4,
            _ => 0,
        };

    private static string GetHistoryPath(BeatmapSet beatmapSet)
    {
        var root = GetExternalsPath();
        var checkRunsPath = Path.Combine(root, CheckRunsFolderName);
        var folderKey = HashString(Path.GetFullPath(beatmapSet.SongPath).ToLowerInvariant());
        return Path.Combine(checkRunsPath, folderKey + ".json");
    }

    private static string GetExternalsPath()
    {
        var folder = OperatingSystem.IsLinux()
            ? Environment.SpecialFolder.LocalApplicationData
            : Environment.SpecialFolder.ApplicationData;

        return Path.Combine(Environment.GetFolderPath(folder), ExternalsFolderName);
    }

    private static StoredCheckRun? ReadPreviousRun(string path)
    {
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<StoredCheckRun>(json, JsonOptions);
    }

    private static void WriteCurrentRun(string path, StoredCheckRun run)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(run, JsonOptions));
    }

    private static string BuildFolderSignature(BeatmapSet beatmapSet)
    {
        var builder = new StringBuilder();

        foreach (
            var filePath in beatmapSet.SongFilePaths.OrderBy(
                p => p,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            var info = new FileInfo(filePath);
            if (!info.Exists)
                continue;

            var relativePath = Path.GetRelativePath(beatmapSet.SongPath, info.FullName);
            builder.Append(relativePath.Replace('\\', '/').ToLowerInvariant());
            builder.Append('|');
            builder.Append(info.Length);
            builder.Append('|');
            builder.Append(info.LastWriteTimeUtc.Ticks);
            builder.AppendLine();
        }

        return HashString(builder.ToString());
    }

    private static string HashString(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed class StoredCheckRun
    {
        public DateTime RunAt { get; set; }
        public string FolderSignature { get; set; } = string.Empty;
        public List<StoredCheckIssue> Issues { get; set; } = [];

        public static StoredCheckRun FromResult(
            DateTime runAt,
            string folderSignature,
            ApiBeatmapSetCheckResult result
        )
        {
            var issues = new List<StoredCheckIssue>();

            AddIssues(issues, result.General.Category, result.General.CheckResults, result.Checks);
            foreach (var difficulty in result.Difficulties)
                AddIssues(issues, difficulty.Category, difficulty.CheckResults, result.Checks);

            return new StoredCheckRun
            {
                RunAt = runAt,
                FolderSignature = folderSignature,
                Issues = issues,
            };
        }

        private static void AddIssues(
            List<StoredCheckIssue> issues,
            string category,
            IEnumerable<ApiCheckResult> checkResults,
            IReadOnlyDictionary<int, ApiCheckDefinition> checks
        )
        {
            foreach (var result in checkResults)
            {
                var hasDefinition = checks.TryGetValue(result.Id, out var definition);
                issues.Add(
                    new StoredCheckIssue
                    {
                        Category = category,
                        Id = result.Id,
                        CheckName = hasDefinition ? definition.Name : $"Check #{result.Id}",
                        Message = result.Message,
                        Level = result.Level,
                    }
                );
            }
        }
    }

    private sealed class StoredCheckIssue
    {
        public string Category { get; set; } = string.Empty;
        public int Id { get; set; }
        public string CheckName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Issue.Level Level { get; set; }

        [JsonIgnore]
        public string Key => $"{Category}\u001f{Id}\u001f{Message}";

        public ApiCheckDeltaIssue ToApiIssue(Issue.Level? previousLevel) =>
            new(Category, Id, CheckName, Message, Level, previousLevel);
    }
}
