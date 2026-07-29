using System.Collections.Concurrent;

namespace MapsetVerifier.Framework;

internal sealed class CheckProgressTracker(int total, IProgress<CheckProgress> progress)
{
    private int _completed;
    private int _nextTaskId;
    private readonly ConcurrentDictionary<int, string> _active = new();

    public int ReportStarted(string label)
    {
        var taskId = Interlocked.Increment(ref _nextTaskId);
        _active[taskId] = label;
        ReportProgress();
        return taskId;
    }

    public void ReportCompleted(int taskId)
    {
        _active.TryRemove(taskId, out _);
        Interlocked.Increment(ref _completed);
        ReportProgress();
    }

    /// <summary>
    ///     Increments the completed count without touching the active label set. For work items
    ///     tracked individually (e.g. one per file) under a single shared "started" label, so the
    ///     label stays visible for the whole batch instead of disappearing after the first item.
    /// </summary>
    public void ReportItemCompleted()
    {
        Interlocked.Increment(ref _completed);
        ReportProgress();
    }

    /// <summary>
    ///     Removes a label from the active set without incrementing the completed count. Pairs with
    ///     <see cref="ReportItemCompleted" /> to close out the shared label once every item tracked
    ///     under it has individually reported completion.
    /// </summary>
    public void ReportLabelFinished(int taskId)
    {
        _active.TryRemove(taskId, out _);
        ReportProgress();
    }

    private void ReportProgress()
    {
        var completed = Volatile.Read(ref _completed);
        var activeLabels = _active.Values.OrderBy(label => label, StringComparer.Ordinal).ToArray();
        progress.Report(new CheckProgress(completed, total, activeLabels));
    }
}
