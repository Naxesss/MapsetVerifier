using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

public static class RankingCriteriaService
{
    private static RcStore Store => RcStore.Embedded;

    /// <summary> Nested statements by the id of the statement they are nested in. </summary>
    private static readonly Lazy<ILookup<string, RcStatement>> Children = new(() =>
        Store
            .AllStatements()
            .Select(entry => entry.Statement)
            .Where(statement => statement.Upstream.Parent != null && !statement.IsRetired)
            .ToLookup(statement => statement.Upstream.Parent!)
    );

    public static ApiRcOverview GetOverview()
    {
        var links = GetLinks();

        var pages = RcPages
            .All.Select(page =>
            {
                var coverage = Enum.GetValues<ApiRcCoverage>()
                    .ToDictionary(status => status, _ => 0);
                foreach (var statement in Store.GetStatements(page.Key).Where(s => !s.IsRetired))
                    coverage[GetCoverage(statement, links)]++;

                return new ApiRcPageSummary(
                    page.Key,
                    page.Title,
                    page.WikiUrl,
                    page.Modes,
                    page.HasStatements,
                    coverage
                );
            })
            .ToList();

        return new ApiRcOverview(GetSource(), pages);
    }

    public static ApiRcPage? GetPage(string key)
    {
        var page = RcPages.Get(key);
        var markdown = Store.GetMarkdown(key);
        if (page == null || markdown == null)
            return null;

        var links = GetLinks();
        var statements = Store
            .GetStatements(key)
            .Where(statement => !statement.IsRetired)
            .Select(statement => ToApi(page, statement, links))
            .ToList();

        return new ApiRcPage(page.Key, page.Title, page.WikiUrl, markdown, statements);
    }

    /// <summary> Looks up statements by id, including retired ones so outdated links can still be explained. </summary>
    public static List<ApiRcStatement> GetStatements(IEnumerable<string> ids)
    {
        var links = GetLinks();

        return ids.Distinct()
            .Select(id =>
                Store.TryGetStatement(id, out var page, out var statement)
                    ? ToApi(page, statement, links)
                    : null
            )
            .OfType<ApiRcStatement>()
            .ToList();
    }

    /// <summary>
    ///     The statements an issue's template links to, limited to those applying to the issue's beatmap mode, so an
    ///     osu! issue from a check shared with osu!catch only points to the osu! rules.
    /// </summary>
    public static IReadOnlyList<string> RuleIdsFor(Issue issue)
    {
        var ruleIds = issue.Template.RuleIds;
        if (issue.beatmap == null)
            return ruleIds;

        var mode = issue.beatmap.GeneralSettings.mode;
        return ruleIds
            .Where(id =>
                !Store.TryGetStatement(id, out var page, out _) || page.Modes.Contains(mode)
            )
            .ToList();
    }

    private static ApiRcSource GetSource() =>
        new(Store.Source.Repository, Store.Source.Commit, Store.Source.CommitDate);

    /// <summary> Every issue template linking to a statement, by statement id. Includes custom checks. </summary>
    private static Dictionary<string, List<ApiRcCheckLink>> GetLinks()
    {
        var links = new Dictionary<string, List<ApiRcCheckLink>>();

        foreach (var (checkId, check) in CheckerRegistry.GetChecksWithId())
        foreach (var (templateKey, template) in check.GetTemplates())
        foreach (var ruleId in template.RuleIds)
        {
            if (!links.TryGetValue(ruleId, out var list))
                links[ruleId] = list = [];

            list.Add(
                new ApiRcCheckLink(
                    checkId,
                    check.GetMetadata().Message,
                    templateKey,
                    template.Level
                )
            );
        }

        return links;
    }

    private static ApiRcCoverage GetCoverage(
        RcStatement statement,
        Dictionary<string, List<ApiRcCheckLink>> links
    )
    {
        if (links.ContainsKey(statement.Id))
            return statement.Curation.Automation == RcAutomation.Partial
                ? ApiRcCoverage.Partial
                : ApiRcCoverage.Covered;

        // Allowances have nothing to enforce, intros are finished by their nested statements.
        if (statement.Upstream.Kind == RcKind.Allowance || statement.Upstream.Intro)
            return ApiRcCoverage.Informational;

        // Statements like "The audio file of a beatmap must..." are enforced through their nested statements.
        var children = Children
            .Value[statement.Id]
            .Select(child => GetCoverage(child, links))
            .Where(coverage =>
                coverage is not (ApiRcCoverage.Informational or ApiRcCoverage.Manual)
            )
            .ToList();

        if (children.Count > 0 && children.Any(coverage => coverage != ApiRcCoverage.Uncovered))
            return children.All(coverage => coverage == ApiRcCoverage.Covered)
                ? ApiRcCoverage.Covered
                : ApiRcCoverage.Partial;

        return statement.Curation.Automation == RcAutomation.Manual
            ? ApiRcCoverage.Manual
            : ApiRcCoverage.Uncovered;
    }

    private static ApiRcStatement ToApi(
        RcPage page,
        RcStatement statement,
        Dictionary<string, List<ApiRcCheckLink>> links
    )
    {
        var upstream = statement.Upstream;
        var wikiUrl =
            upstream.Anchor.Length > 0 ? $"{page.WikiUrl}#{upstream.Anchor}" : page.WikiUrl;

        return new ApiRcStatement(
            statement.Id,
            page.Key,
            page.Title,
            upstream.Kind,
            upstream.Lead,
            upstream.Path,
            wikiUrl,
            upstream.StartLine,
            upstream.EndLine,
            upstream.Parent,
            // Nested statements such as "Maximum width: 2560px" only make sense with their parent.
            upstream.Parent != null
            && Store.TryGetStatement(upstream.Parent, out _, out var parent)
                ? parent.Upstream.Lead
                : null,
            upstream.Intro,
            upstream.Difficulties,
            statement.Curation.Automation,
            statement.Curation.Notes,
            statement.IsRetired,
            GetCoverage(statement, links),
            links.GetValueOrDefault(statement.Id) ?? []
        );
    }
}
