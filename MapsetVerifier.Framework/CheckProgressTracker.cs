namespace MapsetVerifier.Framework;

internal sealed class CheckProgressTracker(int total, IProgress<CheckProgress> progress)
{
    private int _completed;

    public void ReportStarted(string label) =>
        progress.Report(new CheckProgress(Volatile.Read(ref _completed), total, label));

    public void ReportCompleted()
    {
        var completed = Interlocked.Increment(ref _completed);
        progress.Report(new CheckProgress(completed, total, null));
    }
}
