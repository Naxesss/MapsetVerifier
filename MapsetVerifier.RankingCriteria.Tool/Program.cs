using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;
using MapsetVerifier.RankingCriteria.Sync;

namespace MapsetVerifier.RankingCriteria.Tool;

/// <summary>
///     Maintains the ranking criteria snapshot in MapsetVerifier.RankingCriteria/Data.
///     <para />
///     update [--commit &lt;sha&gt;]  Downloads the pages from osu-wiki (latest commit by default) and re-parses them.
///     <br />
///     reparse                     Re-parses the current snapshot, e.g. after changing the parser.
/// </summary>
internal static class Program
{
    private const string Repository = "ppy/osu-wiki";

    private static async Task<int> Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var command = args.FirstOrDefault() ?? "update";
        var commitIndex = Array.IndexOf(args, "--commit");
        var commit =
            commitIndex >= 0 && commitIndex + 1 < args.Length ? args[commitIndex + 1] : null;

        var dataDir = Path.Combine(FindRepoRoot(), "MapsetVerifier.RankingCriteria", "Data");

        try
        {
            switch (command)
            {
                case "update":
                    await Download(dataDir, commit);
                    Reparse(dataDir);
                    return 0;
                case "reparse":
                    Reparse(dataDir);
                    return 0;
                default:
                    Console.Error.WriteLine("Usage: update [--commit <sha>] | reparse");
                    return 1;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static async Task Download(string dataDir, string? commit)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("MapsetVerifier", "1.0")
        );

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        // Latest commit touching the ranking criteria, or the requested one, resolved to a full sha.
        var commitsUrl =
            commit == null
                ? $"https://api.github.com/repos/{Repository}/commits?path=wiki/Ranking_criteria&per_page=1"
                : $"https://api.github.com/repos/{Repository}/commits/{commit}";

        using var commitJson = JsonDocument.Parse(await http.GetStringAsync(commitsUrl));
        var commitElement = commit == null ? commitJson.RootElement[0] : commitJson.RootElement;
        var sha = commitElement.GetProperty("sha").GetString()!;
        var date = commitElement
            .GetProperty("commit")
            .GetProperty("committer")
            .GetProperty("date")
            .GetDateTimeOffset();

        var sourcePath = Path.Combine(dataDir, "source.json");
        if (File.Exists(sourcePath))
        {
            var current = JsonSerializer.Deserialize<RcSource>(
                await File.ReadAllTextAsync(sourcePath),
                RcJson.Options
            );
            if (current?.Commit == sha)
            {
                Console.Error.WriteLine(
                    $"Already at {Repository}@{sha[..8]}, nothing to download."
                );
                return;
            }
        }

        Console.Error.WriteLine($"Snapshotting {Repository}@{sha[..8]} ({date:yyyy-MM-dd})");

        var snapshotsDir = Path.Combine(dataDir, "snapshots");
        var snapshotDir = Path.Combine(snapshotsDir, sha);
        Directory.CreateDirectory(snapshotDir);

        foreach (var page in RcPages.All)
        {
            var url =
                $"https://raw.githubusercontent.com/{Repository}/{sha}/{Uri.EscapeDataString(page.SourcePath).Replace("%2F", "/")}";
            var markdown = await http.GetStringAsync(url);
            await File.WriteAllTextAsync(Path.Combine(snapshotDir, page.Key + ".md"), markdown);
            Console.Error.WriteLine($"  {page.SourcePath} ({markdown.Length} chars)");
        }

        // Only the current snapshot is kept for now; older ones stay available through git history.
        foreach (
            var dir in Directory
                .GetDirectories(snapshotsDir)
                .Where(dir => Path.GetFileName(dir) != sha)
        )
            Directory.Delete(dir, true);

        var source = new RcSource
        {
            Repository = Repository,
            Commit = sha,
            CommitDate = date,
            FetchedAt = DateTimeOffset.UtcNow,
        };
        await File.WriteAllTextAsync(
            sourcePath,
            JsonSerializer.Serialize(source, RcJson.Options) + "\n"
        );
    }

    private static void Reparse(string dataDir)
    {
        var source = JsonSerializer.Deserialize<RcSource>(
            File.ReadAllText(Path.Combine(dataDir, "source.json")),
            RcJson.Options
        )!;
        var snapshotDir = Path.Combine(dataDir, "snapshots", source.Commit);
        var catalogueDir = Path.Combine(dataDir, "catalogue");
        Directory.CreateDirectory(catalogueDir);

        var pages = new List<RcCataloguePage>();
        var allChanges = new List<RcChange>();

        foreach (var page in RcPages.All.Where(page => page.HasStatements))
        {
            var cataloguePath = Path.Combine(catalogueDir, page.Key + ".json");
            var existing = File.Exists(cataloguePath)
                ? JsonSerializer.Deserialize<RcCataloguePage>(
                    File.ReadAllText(cataloguePath),
                    RcJson.Options
                )
                : null;

            var parsed = RcMarkdownParser.Parse(
                File.ReadAllText(Path.Combine(snapshotDir, page.Key + ".md"))
            );
            var result = RcCatalogueMerger.Merge(page, existing, parsed, source.Commit);

            File.WriteAllText(
                cataloguePath,
                JsonSerializer.Serialize(result.Page, RcJson.Options) + "\n"
            );
            pages.Add(result.Page);
            allChanges.AddRange(result.Changes);

            var active = result.Page.Statements.Count(statement => !statement.IsRetired);
            Console.Error.WriteLine(
                $"{page.Key}: {active} statements, {result.Changes.Count} changes"
            );
        }

        var constantsPath = Path.Combine(dataDir, "..", "RC.g.cs");
        File.WriteAllText(constantsPath, RcConstantsWriter.Write(source, pages));

        // The change report goes to stdout so it can be piped into a pull request description later.
        foreach (var group in allChanges.GroupBy(change => change.Type).OrderBy(group => group.Key))
        {
            Console.WriteLine($"## {group.Key} ({group.Count()})");
            foreach (var change in group)
                Console.WriteLine($"- `{change.Id}`: {change.Detail}");
            Console.WriteLine();
        }
    }

    /// <summary> Finds the repository root by walking up until MapsetVerifier.slnx is found. </summary>
    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "MapsetVerifier.slnx")))
                    return dir.FullName;

        throw new InvalidOperationException(
            "Could not find the repository root (MapsetVerifier.slnx)."
        );
    }
}
