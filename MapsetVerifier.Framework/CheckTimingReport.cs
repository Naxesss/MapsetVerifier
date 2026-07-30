namespace MapsetVerifier.Framework;

/// <summary>
///     Per-check timings for a single <see cref="Checker.GetBeatmapSetIssues" /> run, plus the actual
///     wall-clock duration of the whole run. Since checks execute in parallel, the sum of individual
///     <see cref="CheckTiming.ElapsedMs" /> values will generally exceed <see cref="TotalElapsedMs" />.
/// </summary>
public sealed class CheckTimingReport
{
    public List<CheckTiming> Checks { get; init; } = [];
    public long TotalElapsedMs { get; init; }
}
