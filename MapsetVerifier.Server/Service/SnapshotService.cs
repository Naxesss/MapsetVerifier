using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Server.Service;

public static class SnapshotService
{
    /// <summary> Stores the mapset's current state if it changed. </summary>
    public static SnapshotEntry? CaptureCurrent(
        BeatmapSet beatmapSet,
        string trigger,
        string? pin = null
    ) => beatmapSet.Beatmaps.Count == 0 ? null : SnapshotStore.Capture(beatmapSet, trigger, pin);

    /// <summary> Keeps the numbers of the check run that just happened with the latest snapshot. </summary>
    public static void AttachChecks(BeatmapSet beatmapSet, ApiBeatmapSetCheckResult result)
    {
        if (beatmapSet.Beatmaps.Count == 0)
            return;

        var results = result
            .Difficulties.Append(result.General)
            .SelectMany(c => c.CheckResults)
            .ToList();

        SnapshotStore.AttachChecks(
            SnapshotStore.ResolveSetKey(beatmapSet),
            new StoredChecks
            {
                Problems = results.Count(r => r.Level == Issue.Level.Problem),
                Warnings = results.Count(r => r.Level == Issue.Level.Warning),
                Minor = results.Count(r => r.Level == Issue.Level.Minor),
            }
        );
    }

    /// <summary> Captures the mapset as it is now, then lists its snapshots. </summary>
    public static HistoryResult GetHistory(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);

        if (beatmapSet.Beatmaps.Count == 0)
            return new HistoryResult("", [], []);

        CaptureCurrent(beatmapSet, "pageOpen");

        return SnapshotHistory.Build(SnapshotStore.ResolveSetKey(beatmapSet));
    }

    public static ComparisonResult Compare(string setKey, string baseId, string targetId)
    {
        var log = SnapshotStore.Load(setKey);
        var before = log.Entries.FirstOrDefault(e => e.Id == baseId);
        var after = log.Entries.FirstOrDefault(e => e.Id == targetId);

        if (before == null || after == null)
            throw new ArgumentException("Unknown snapshot.");

        return SnapshotComparer.Compare(setKey, before, after);
    }

    /// <summary> A few objects of one difficulty around a change, before and after. </summary>
    public static WindowResult GetWindow(
        string setKey,
        string baseId,
        string targetId,
        string difficultyKey,
        double anchor,
        double anchorEnd,
        int offset
    )
    {
        var log = SnapshotStore.Load(setKey);
        var before = log.Entries.FirstOrDefault(e => e.Id == baseId);
        var after = log.Entries.FirstOrDefault(e => e.Id == targetId);

        if (before == null || after == null)
            throw new ArgumentException("Unknown snapshot.");

        return SnapshotComparer.GetWindow(
            setKey,
            before,
            after,
            difficultyKey,
            anchor,
            anchorEnd,
            offset
        );
    }

    /// <summary> A manual snapshot, optionally pinned as a milestone. </summary>
    public static HistoryResult Capture(string beatmapSetFolder, string? pin)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);

        if (beatmapSet.Beatmaps.Count == 0)
            return new HistoryResult("", [], []);

        CaptureCurrent(beatmapSet, "manual", string.IsNullOrWhiteSpace(pin) ? null : pin.Trim());

        return SnapshotHistory.Build(SnapshotStore.ResolveSetKey(beatmapSet));
    }

    public static HistoryResult SetPin(string setKey, string id, string? pin)
    {
        if (!SnapshotStore.SetPin(setKey, id, pin))
            throw new ArgumentException("Unknown snapshot.");

        return SnapshotHistory.Build(setKey);
    }
}
