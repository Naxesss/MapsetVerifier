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

    private void ReportProgress()
    {
        var completed = Volatile.Read(ref _completed);
        var activeLabels = _active.Values.OrderBy(label => label, StringComparer.Ordinal).ToArray();
        progress.Report(new CheckProgress(completed, total, activeLabels));
    }
}
