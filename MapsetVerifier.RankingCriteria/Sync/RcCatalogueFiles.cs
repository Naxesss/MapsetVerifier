using System.Text.Json;
using MapsetVerifier.RankingCriteria.Model;

namespace MapsetVerifier.RankingCriteria.Sync;

/// <summary>
///     Reads and writes the catalogue in a source checkout's data directory, for the tool and the Debug backend.
/// </summary>
public static class RcCatalogueFiles
{
    public static string PagePath(string dataDir, string pageKey) =>
        Path.Combine(dataDir, "catalogue", pageKey + ".json");

    public static RcCataloguePage ReadPage(string dataDir, string pageKey) =>
        JsonSerializer.Deserialize<RcCataloguePage>(
            File.ReadAllText(PagePath(dataDir, pageKey)),
            RcJson.Options
        )!;

    public static void WritePage(string dataDir, RcCataloguePage page) =>
        File.WriteAllText(
            PagePath(dataDir, page.Page),
            JsonSerializer.Serialize(page, RcJson.Options) + "\n"
        );

    /// <summary> Every page with statements, in the order of <see cref="RcPages.All" />. </summary>
    public static List<RcCataloguePage> ReadPages(string dataDir) =>
        RcPages
            .All.Where(page => page.HasStatements)
            .Select(page => ReadPage(dataDir, page.Key))
            .ToList();

    /// <summary> Re-writes RC.g.cs next to the data directory from the given pages. </summary>
    public static void WriteConstants(string dataDir, IEnumerable<RcCataloguePage> pages)
    {
        var source = JsonSerializer.Deserialize<RcSource>(
            File.ReadAllText(Path.Combine(dataDir, "source.json")),
            RcJson.Options
        )!;
        File.WriteAllText(
            Path.Combine(dataDir, "..", "RC.g.cs"),
            RcConstantsWriter.Write(source, pages)
        );
    }

    /// <summary> Statements whose wording or kind changed since their checks were last reviewed. </summary>
    public static IEnumerable<RcStatement> PendingReview(RcCataloguePage page) =>
        page.Statements.Where(statement => statement.LastReview != null && !statement.IsRetired);

    /// <summary>
    ///     Clears the last review of the given statements and re-writes the RC constants, which drops the obsolete
    ///     warning from the checks linking to them. Returns the ids that were waiting for a review.
    /// </summary>
    public static List<string> MarkReviewed(string dataDir, IReadOnlyCollection<string> ids)
    {
        var pages = ReadPages(dataDir);
        var reviewed = new List<string>();

        foreach (var page in pages)
        {
            var pending = PendingReview(page)
                .Where(statement => ids.Contains(statement.Id))
                .ToList();
            if (pending.Count == 0)
                continue;

            foreach (var statement in pending)
            {
                statement.LastReview = null;
                reviewed.Add(statement.Id);
            }

            WritePage(dataDir, page);
        }

        if (reviewed.Count > 0)
            WriteConstants(dataDir, pages);

        return reviewed;
    }
}
