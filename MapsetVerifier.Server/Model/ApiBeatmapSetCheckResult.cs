using MapsetVerifier.Framework;

namespace MapsetVerifier.Server.Model;

public readonly struct ApiBeatmapSetCheckResult(
    ApiCategoryCheckResult general,
    IEnumerable<ApiCategoryCheckResult> difficulties,
    Dictionary<int, ApiCheckDefinition> checks,
    ApiCheckRunDelta? checkRunDelta = null,
    CheckTimingReport? checkTimings = null
)
{
    public ApiCategoryCheckResult General { get; } = general;
    public IEnumerable<ApiCategoryCheckResult> Difficulties { get; } = difficulties;
    public Dictionary<int, ApiCheckDefinition> Checks { get; } = checks;
    public ApiCheckRunDelta? CheckRunDelta { get; } = checkRunDelta;

    /// <summary> Populated only when the request opts in via <c>IncludeCheckTimings</c>. </summary>
    public CheckTimingReport? CheckTimings { get; } = checkTimings;
}
