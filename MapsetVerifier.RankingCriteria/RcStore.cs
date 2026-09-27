using System.Reflection;
using System.Text.Json;
using MapsetVerifier.RankingCriteria.Model;

namespace MapsetVerifier.RankingCriteria;

/// <summary>
///     Read access to the ranking criteria snapshot and catalogue embedded in this assembly. Loaded once, lazily.
/// </summary>
public sealed class RcStore
{
    private const string ResourcePrefix = "rc/";

    private static readonly Lazy<RcStore> LazyEmbedded = new(LoadEmbedded);

    private readonly Dictionary<string, string> markdownByPage;
    private readonly Dictionary<string, RcCataloguePage> catalogueByPage;
    private readonly Dictionary<string, (RcPage Page, RcStatement Statement)> statementsById;

    public RcStore(
        RcSource source,
        Dictionary<string, string> markdownByPage,
        Dictionary<string, RcCataloguePage> catalogueByPage
    )
    {
        Source = source;
        this.markdownByPage = markdownByPage;
        this.catalogueByPage = catalogueByPage;

        statementsById = [];
        foreach (var (pageKey, cataloguePage) in catalogueByPage)
        {
            var page = RcPages.Get(pageKey);
            if (page == null)
                continue;

            foreach (var statement in cataloguePage.Statements)
                statementsById[statement.Id] = (page, statement);
        }
    }

    /// <summary> The snapshot and catalogue shipped with this build. </summary>
    public static RcStore Embedded => LazyEmbedded.Value;

    public RcSource Source { get; }

    public string? GetMarkdown(string pageKey) => markdownByPage.GetValueOrDefault(pageKey);

    /// <summary> All statements of a page, including retired ones. </summary>
    public IReadOnlyList<RcStatement> GetStatements(string pageKey) =>
        catalogueByPage.TryGetValue(pageKey, out var page) ? page.Statements : [];

    public IEnumerable<(RcPage Page, RcStatement Statement)> AllStatements() =>
        statementsById.Values;

    public bool TryGetStatement(string id, out RcPage page, out RcStatement statement)
    {
        if (statementsById.TryGetValue(id, out var entry))
        {
            page = entry.Page;
            statement = entry.Statement;
            return true;
        }

        page = null!;
        statement = null!;
        return false;
    }

    private static RcStore LoadEmbedded()
    {
        var assembly = typeof(RcStore).Assembly;
        var resources = assembly
            .GetManifestResourceNames()
            // RecursiveDir uses backslashes when built on Windows.
            .ToDictionary(name => name.Replace('\\', '/'), name => name);

        var source =
            Read<RcSource>(assembly, resources, ResourcePrefix + "source.json") ?? new RcSource();

        var markdown = new Dictionary<string, string>();
        var catalogue = new Dictionary<string, RcCataloguePage>();

        foreach (var page in RcPages.All)
        {
            var markdownName = $"{ResourcePrefix}snapshots/{source.Commit}/{page.Key}.md";
            if (resources.TryGetValue(markdownName, out var markdownResource))
                markdown[page.Key] = ReadText(assembly, markdownResource);

            var cataloguePage = Read<RcCataloguePage>(
                assembly,
                resources,
                $"{ResourcePrefix}catalogue/{page.Key}.json"
            );
            if (cataloguePage != null)
                catalogue[page.Key] = cataloguePage;
        }

        return new RcStore(source, markdown, catalogue);
    }

    private static T? Read<T>(Assembly assembly, Dictionary<string, string> resources, string name)
    {
        return resources.TryGetValue(name, out var resource)
            ? JsonSerializer.Deserialize<T>(ReadText(assembly, resource), RcJson.Options)
            : default;
    }

    private static string ReadText(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
