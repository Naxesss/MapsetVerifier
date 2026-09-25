using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria.Model;

namespace MapsetVerifier.Server.Model;

/// <summary> How well a ranking criteria statement is covered by checks. </summary>
public enum ApiRcCoverage
{
    /// <summary> At least one issue template links to the statement. </summary>
    Covered,

    /// <summary> Linked, but marked as only partially verifiable by a check. </summary>
    Partial,

    /// <summary> Linked, but its wording or kind changed on the wiki since the checks were reviewed. </summary>
    Outdated,

    /// <summary> No issue template links to the statement yet. </summary>
    Uncovered,

    /// <summary> Marked as requiring human judgement. </summary>
    Manual,

    /// <summary> Allowances and intros such as "The audio file of a beatmap must..." have nothing to enforce. </summary>
    Informational,
}

public sealed record ApiRcSource(string Repository, string Commit, DateTimeOffset? CommitDate);

public sealed record ApiRcOverview(ApiRcSource Source, List<ApiRcPageSummary> Pages);

public sealed record ApiRcPageSummary(
    string Key,
    string Title,
    string WikiUrl,
    Beatmap.Mode[] Modes,
    bool HasStatements,
    Dictionary<ApiRcCoverage, int> Coverage
);

public sealed record ApiRcPage(
    string Key,
    string Title,
    string WikiUrl,
    string Markdown,
    List<ApiRcStatement> Statements
);

public sealed record ApiRcCheckLink(
    int CheckId,
    string CheckName,
    string TemplateKey,
    Issue.Level Level
);

/// <summary> How a statement read when its linked checks were last reviewed, if it changed since. </summary>
public sealed record ApiRcReview(string Commit, RcKind Kind);

public sealed record ApiRcStatement(
    string Id,
    string Page,
    string PageTitle,
    RcKind Kind,
    string Lead,
    List<string> Path,
    string WikiUrl,
    int StartLine,
    int EndLine,
    string? ParentId,
    string? ParentLead,
    bool Intro,
    List<Beatmap.Difficulty> Difficulties,
    RcAutomation Automation,
    string? Notes,
    bool Retired,
    ApiRcReview? LastReview,
    ApiRcCoverage Coverage,
    List<ApiRcCheckLink> Links
);
