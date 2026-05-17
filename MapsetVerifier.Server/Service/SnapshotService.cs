using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Snapshots;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Server.Service;

public static class SnapshotService
{
    public static ApiSnapshotResult GetSnapshots(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var refBeatmap = beatmapSet.Beatmaps[0];

        // Build difficulties list
        var difficulties = new List<ApiSnapshotDifficulty>
        {
            new("General", isGeneral: true, starRating: null, mode: null),
        };
        difficulties.AddRange(
            beatmapSet.Beatmaps.Select(beatmap => new ApiSnapshotDifficulty(
                beatmap.MetadataSettings.version,
                isGeneral: false,
                starRating: beatmap.StarRating,
                mode: beatmap.GeneralSettings.mode
            ))
        );

        // Check if beatmapset ID is valid
        if (refBeatmap.MetadataSettings.beatmapSetId == null)
        {
            return new ApiSnapshotResult(
                difficulties: difficulties,
                general: null,
                beatmapHistories: [],
                errorMessage: "The BeatmapSet ID is -1, indicating that the map is not submitted. This makes the snapshotter not work. Either download the submitted version from the osu! website, or submit the map before using this feature."
            );
        }

        var beatmapSetId = refBeatmap.MetadataSettings.beatmapSetId.ToString();
        if (beatmapSetId == null)
        {
            return new ApiSnapshotResult(
                difficulties: difficulties,
                general: null,
                beatmapHistories: [],
                errorMessage: null
            );
        }

        // Snapshot the current beatmap before we start comparing with previous snapshots
        Snapshotter.SnapshotBeatmapSet(beatmapSet);

        // Get general/files snapshot history
        var refSnapshots = Snapshotter.GetSnapshots(beatmapSetId, "files").ToArray();
        var generalHistory = GetGeneralSnapshotHistory(refSnapshots);

        // Unified timeline so difficulties unchanged in an update still get a commit (issue #98).
        var globalTimeline = BuildGlobalSnapshotTimeline(beatmapSet, beatmapSetId);
        var beatmapHistories = beatmapSet
            .Beatmaps.Select(beatmap => GetBeatmapSnapshotHistory(beatmap, globalTimeline))
            .ToList();

        return new ApiSnapshotResult(
            difficulties: difficulties,
            general: generalHistory,
            beatmapHistories: beatmapHistories,
            errorMessage: null
        );
    }

    private static ApiSnapshotHistory GetGeneralSnapshotHistory(Snapshotter.Snapshot[] snapshots)
    {
        if (snapshots.Length == 0)
        {
            return new ApiSnapshotHistory("General", []);
        }

        // Sort snapshots by date (oldest first for comparison, then reverse for display)
        var sortedSnapshots = snapshots.OrderBy(s => s.creationTime).ToArray();
        var commits = new List<ApiSnapshotCommit>();

        // Compare each snapshot to the previous one
        for (var i = 1; i < sortedSnapshots.Length; i++)
        {
            var previousSnapshot = sortedSnapshots[i - 1];
            var currentSnapshot = sortedSnapshots[i];

            var diffs = Snapshotter.Compare(previousSnapshot, currentSnapshot.code).ToList();

            // TODO null! is not very nice needs quite some refactor in snapshotter logic to avoid
            var translatedDiffs = Snapshotter.TranslateComparison(diffs, null!).ToList();

            if (translatedDiffs.Count == 0)
                continue;

            var commit = CreateCommit(
                currentSnapshot.creationTime,
                translatedDiffs,
                filesOnly: true
            );
            commits.Add(commit);
        }

        // Reverse to show newest first
        commits.Reverse();

        return new ApiSnapshotHistory("General", commits);
    }

    /// <summary>
    /// Snapshot times for the whole mapset (.osu snapshots plus files snapshots) sorted oldest-first.
    /// Aligns per-difficulty history so an update that only touches other difficulties still appears on every chart.
    /// </summary>
    private static DateTime[] BuildGlobalSnapshotTimeline(
        BeatmapSet beatmapSet,
        string beatmapSetId
    )
    {
        var times = new HashSet<DateTime>();
        foreach (var bm in beatmapSet.Beatmaps)
        {
            foreach (var s in Snapshotter.GetSnapshots(bm))
                times.Add(s.creationTime);
        }

        foreach (var s in Snapshotter.GetSnapshots(beatmapSetId, "files"))
            times.Add(s.creationTime);

        return times.OrderBy(t => t).ToArray();
    }

    /// <summary>
    /// Latest stored snapshot for this beatmap with <see cref="Snapshotter.Snapshot.creationTime"/> &lt;= <paramref name="t"/>.
    /// </summary>
    private static Snapshotter.Snapshot? GetLatestSnapshotAtOrBefore(
        Snapshotter.Snapshot[] sortedOldestFirst,
        DateTime t
    )
    {
        Snapshotter.Snapshot? best = null;
        foreach (var s in sortedOldestFirst)
        {
            if (s.creationTime <= t)
                best = s;
            else
                break;
        }

        return best;
    }

    private static ApiSnapshotHistory GetBeatmapSnapshotHistory(
        Beatmap beatmap,
        IReadOnlyList<DateTime> globalTimeline
    )
    {
        var snapshots = Snapshotter.GetSnapshots(beatmap).OrderBy(s => s.creationTime).ToArray();

        if (snapshots.Length == 0)
        {
            return new ApiSnapshotHistory(beatmap.MetadataSettings.version, []);
        }

        var commits = new List<ApiSnapshotCommit>();

        for (var i = 1; i < globalTimeline.Count; i++)
        {
            var tPrev = globalTimeline[i - 1];
            var tCurr = globalTimeline[i];
            var snapPrev = GetLatestSnapshotAtOrBefore(snapshots, tPrev);
            var snapCurr = GetLatestSnapshotAtOrBefore(snapshots, tCurr);

            // Match legacy behaviour: no commit before this difficulty has any snapshot.
            if (snapPrev is null || snapCurr is null)
                continue;

            var diffs = Snapshotter.Compare(snapPrev.Value, snapCurr.Value.code).ToList();
            var translatedDiffs = Snapshotter.TranslateComparison(diffs, beatmap).ToList();
            commits.Add(CreateCommit(tCurr, translatedDiffs, filesOnly: false));
        }

        // Also compare the latest snapshot to current code (uncommitted changes)
        var latestSnapshot = snapshots.Last();
        var currentDiffs = Snapshotter.Compare(latestSnapshot, beatmap.Code).ToList();
        var translatedCurrentDiffs = Snapshotter
            .TranslateComparison(currentDiffs, beatmap)
            .ToList();

        if (translatedCurrentDiffs.Count > 0)
        {
            var uncommittedCommit = CreateCommit(
                DateTime.UtcNow,
                translatedCurrentDiffs,
                filesOnly: false
            );
            commits.Add(uncommittedCommit);
        }

        // Reverse to show newest first
        commits.Reverse();

        return new ApiSnapshotHistory(beatmap.MetadataSettings.version, commits);
    }

    private static ApiSnapshotCommit CreateCommit(
        DateTime date,
        List<DiffInstance> diffs,
        bool filesOnly
    )
    {
        var filteredDiffs = diffs
            .Where(diff => filesOnly ? diff.Section == "Files" : diff.Section != "Files")
            .Where(diff => !string.IsNullOrWhiteSpace(diff.Diff)) // Filter out empty diffs (e.g., empty lines)
            .ToList();

        var additions = filteredDiffs.Count(d => d.DiffType == Snapshotter.DiffType.Added);
        var removals = filteredDiffs.Count(d => d.DiffType == Snapshotter.DiffType.Removed);
        var modifications = filteredDiffs.Count(d => d.DiffType == Snapshotter.DiffType.Changed);

        var sections = filteredDiffs
            .GroupBy(diff => diff.Section)
            .Select(sectionDiffs =>
            {
                var sectionDiffsList = sectionDiffs.ToList();
                var sectionAdditions = sectionDiffsList.Count(d =>
                    d.DiffType == Snapshotter.DiffType.Added
                );
                var sectionRemovals = sectionDiffsList.Count(d =>
                    d.DiffType == Snapshotter.DiffType.Removed
                );
                var sectionModifications = sectionDiffsList.Count(d =>
                    d.DiffType == Snapshotter.DiffType.Changed
                );

                return new ApiSnapshotSection(
                    name: sectionDiffsList.First().Section,
                    aggregatedDiffType: GetAggregatedDiffType(sectionDiffsList),
                    additions: sectionAdditions,
                    removals: sectionRemovals,
                    modifications: sectionModifications,
                    diffs: sectionDiffsList.Select(diff => new ApiSnapshotDiff(
                        message: diff.Diff,
                        diffType: diff.DiffType,
                        oldValue: ExtractOldValue(diff),
                        newValue: ExtractNewValue(diff),
                        details: diff.Details
                    ))
                );
            })
            .ToList();

        return new ApiSnapshotCommit(
            date: date,
            id: date.ToString("yyyyMMddHHmmss"),
            totalChanges: filteredDiffs.Count,
            additions: additions,
            removals: removals,
            modifications: modifications,
            sections: sections
        );
    }

    private static string? ExtractOldValue(DiffInstance diff)
    {
        // Try to extract old value from the message for "Changed" diffs
        // Format: "X was changed from \"old\" to \"new\""
        if (diff.DiffType == Snapshotter.DiffType.Changed && diff.Diff.Contains("was changed from"))
        {
            var fromIndex = diff.Diff.IndexOf("from \"", StringComparison.Ordinal);
            var toIndex = diff.Diff.IndexOf("\" to \"", StringComparison.Ordinal);
            if (fromIndex >= 0 && toIndex > fromIndex)
            {
                return diff.Diff.Substring(fromIndex + 6, toIndex - fromIndex - 6);
            }
        }
        // For removed diffs, the message often contains the value
        // Format: "X was removed and is no longer set to \"value\""
        if (diff.DiffType == Snapshotter.DiffType.Removed && diff.Diff.Contains("set to \""))
        {
            var startIndex = diff.Diff.LastIndexOf("set to \"", StringComparison.Ordinal);
            if (startIndex >= 0)
            {
                var valueStart = startIndex + 8;
                var valueEnd = diff.Diff.LastIndexOf("\"", StringComparison.Ordinal);
                if (valueEnd > valueStart)
                {
                    return diff.Diff.Substring(valueStart, valueEnd - valueStart);
                }
            }
        }
        return null;
    }

    private static string? ExtractNewValue(DiffInstance diff)
    {
        // Try to extract new value from the message for "Changed" diffs
        // Format: "X was changed from \"old\" to \"new\""
        if (diff.DiffType == Snapshotter.DiffType.Changed && diff.Diff.Contains("\" to \""))
        {
            var toIndex = diff.Diff.IndexOf("\" to \"", StringComparison.Ordinal);
            if (toIndex >= 0)
            {
                var valueStart = toIndex + 6;
                var valueEnd = diff.Diff.LastIndexOf("\"", StringComparison.Ordinal);
                if (valueEnd > valueStart)
                {
                    return diff.Diff.Substring(valueStart, valueEnd - valueStart);
                }
            }
        }
        // For added diffs, the message often contains the value
        // Format: "X was added and set to \"value\""
        if (diff.DiffType == Snapshotter.DiffType.Added && diff.Diff.Contains("set to \""))
        {
            var startIndex = diff.Diff.LastIndexOf("set to \"", StringComparison.Ordinal);
            if (startIndex >= 0)
            {
                var valueStart = startIndex + 8;
                var valueEnd = diff.Diff.LastIndexOf("\"", StringComparison.Ordinal);
                if (valueEnd > valueStart)
                {
                    return diff.Diff.Substring(valueStart, valueEnd - valueStart);
                }
            }
        }
        return null;
    }

    private static Snapshotter.DiffType GetAggregatedDiffType(IEnumerable<DiffInstance> diffs)
    {
        var diffsList = diffs.ToArray();
        var hasAdded = diffsList.Any(diff => diff.DiffType == Snapshotter.DiffType.Added);
        var hasRemoved = diffsList.Any(diff => diff.DiffType == Snapshotter.DiffType.Removed);
        var hasChanged = diffsList.Any(diff => diff.DiffType == Snapshotter.DiffType.Changed);

        if ((hasAdded && hasRemoved) || hasChanged)
            return Snapshotter.DiffType.Changed;
        if (hasAdded)
            return Snapshotter.DiffType.Added;
        if (hasRemoved)
            return Snapshotter.DiffType.Removed;

        return Snapshotter.DiffType.Changed;
    }
}
