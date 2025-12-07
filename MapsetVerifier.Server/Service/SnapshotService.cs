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
            new("General", isGeneral: true, starRating: null, mode: null)
        };
        difficulties.AddRange(beatmapSet.Beatmaps.Select(beatmap =>
            new ApiSnapshotDifficulty(beatmap.MetadataSettings.version, isGeneral: false, starRating: beatmap.StarRating, mode: beatmap.GeneralSettings.mode)));

        // Extract metadata for header
        var title = refBeatmap.MetadataSettings.title;
        var artist = refBeatmap.MetadataSettings.artist;
        var creator = refBeatmap.MetadataSettings.creator;

        // Check if beatmapset ID is valid
        if (refBeatmap.MetadataSettings.beatmapSetId == null)
        {
            return new ApiSnapshotResult(
                title: title,
                artist: artist,
                creator: creator,
                difficulties: difficulties,
                general: null,
                beatmapHistories: [],
                errorMessage: "The BeatmapSet ID is -1, indicating that the map is not submitted. This makes the snapshotter not work. Either download the submitted version from the osu! website, or submit the map before using this feature.");
        }

        var beatmapSetId = refBeatmap.MetadataSettings.beatmapSetId.ToString();
        if (beatmapSetId == null)
        {
            return new ApiSnapshotResult(
                title: title,
                artist: artist,
                creator: creator,
                difficulties: difficulties,
                general: null,
                beatmapHistories: [],
                errorMessage: null);
        }

        // Get general/files snapshot history
        var refSnapshots = Snapshotter.GetSnapshots(beatmapSetId, "files").ToArray();
        var generalHistory = GetGeneralSnapshotHistory(refSnapshots);

        // Get beatmap snapshot histories
        var beatmapHistories = beatmapSet.Beatmaps.Select(GetBeatmapSnapshotHistory).ToList();

        return new ApiSnapshotResult(
            title: title,
            artist: artist,
            creator: creator,
            difficulties: difficulties,
            general: generalHistory,
            beatmapHistories: beatmapHistories,
            errorMessage: null);
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
            var translatedDiffs = Snapshotter.TranslateComparison(diffs).ToList();

            if (translatedDiffs.Count == 0) continue;

            var commit = CreateCommit(currentSnapshot.creationTime, translatedDiffs, filesOnly: true);
            commits.Add(commit);
        }

        // Reverse to show newest first
        commits.Reverse();

        return new ApiSnapshotHistory("General", commits);
    }

    private static ApiSnapshotHistory GetBeatmapSnapshotHistory(Beatmap beatmap)
    {
        var snapshots = Snapshotter.GetSnapshots(beatmap).ToArray();

        if (snapshots.Length == 0)
        {
            return new ApiSnapshotHistory(beatmap.MetadataSettings.version, []);
        }

        // Sort snapshots by date (oldest first for comparison)
        var sortedSnapshots = snapshots.OrderBy(s => s.creationTime).ToArray();
        var commits = new List<ApiSnapshotCommit>();

        // Compare each snapshot to the previous one
        for (var i = 1; i < sortedSnapshots.Length; i++)
        {
            var previousSnapshot = sortedSnapshots[i - 1];
            var currentSnapshot = sortedSnapshots[i];

            var diffs = Snapshotter.Compare(previousSnapshot, currentSnapshot.code).ToList();
            var translatedDiffs = Snapshotter.TranslateComparison(diffs).ToList();

            if (translatedDiffs.Count == 0) continue;

            var commit = CreateCommit(currentSnapshot.creationTime, translatedDiffs, filesOnly: false);
            commits.Add(commit);
        }

        // Also compare the latest snapshot to current code (uncommitted changes)
        if (sortedSnapshots.Length > 0)
        {
            var latestSnapshot = sortedSnapshots.Last();
            var currentDiffs = Snapshotter.Compare(latestSnapshot, beatmap.Code).ToList();
            var translatedCurrentDiffs = Snapshotter.TranslateComparison(currentDiffs).ToList();

            if (translatedCurrentDiffs.Count > 0)
            {
                var uncommittedCommit = CreateCommit(DateTime.UtcNow, translatedCurrentDiffs, filesOnly: false);
                commits.Add(uncommittedCommit);
            }
        }

        // Reverse to show newest first
        commits.Reverse();

        return new ApiSnapshotHistory(beatmap.MetadataSettings.version, commits);
    }

    private static ApiSnapshotCommit CreateCommit(DateTime date, List<DiffInstance> diffs, bool filesOnly)
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
                var sectionAdditions = sectionDiffsList.Count(d => d.DiffType == Snapshotter.DiffType.Added);
                var sectionRemovals = sectionDiffsList.Count(d => d.DiffType == Snapshotter.DiffType.Removed);
                var sectionModifications = sectionDiffsList.Count(d => d.DiffType == Snapshotter.DiffType.Changed);

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
                        details: diff.Details)));
            })
            .ToList();

        return new ApiSnapshotCommit(
            date: date,
            id: date.ToString("yyyyMMddHHmmss"),
            totalChanges: filteredDiffs.Count,
            additions: additions,
            removals: removals,
            modifications: modifications,
            sections: sections);
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
