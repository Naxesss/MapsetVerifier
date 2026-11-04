using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Snapshots.Compare
{
    /// <summary>
    /// The list of snapshots of a mapset with what changed in each, newest first. Summaries are
    /// worked out once per snapshot and stored, so an old mapset is only slow the first time.
    /// </summary>
    public static class SnapshotHistory
    {
        /// <summary>
        /// Raise this whenever the diff changes what it counts, so stored summaries made by the
        /// older one are worked out again instead of disagreeing with the comparison.
        /// </summary>
        public const int SummaryVersion = 2;

        public static HistoryResult Build(string setKey)
        {
            var log = SnapshotStore.Load(setKey);
            var entries = log.Entries;
            var summaries = new Dictionary<string, StoredSummary>();

            var missing = entries
                .Select((entry, index) => (Entry: entry, Index: index))
                .Where(x => x.Entry.Summary?.Version != SummaryVersion)
                .ToList();

            var computed = new System.Collections.Concurrent.ConcurrentDictionary<
                string,
                StoredSummary
            >();

            Parallel.ForEach(
                missing,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                x =>
                {
                    computed[x.Entry.Id] =
                        x.Index == 0
                            ? new StoredSummary { Version = SummaryVersion }
                            : Summarize(setKey, entries[x.Index - 1], x.Entry);
                }
            );

            foreach (var (id, summary) in computed)
                summaries[id] = summary;

            SnapshotStore.SaveSummaries(setKey, summaries);

            // The picker shows the latest snapshot's ratings; imported snapshots did not store them.
            var latestDifficulties = entries.LastOrDefault()?.Difficulties ?? [];
            var missingStars = latestDifficulties
                .Where(d => d.Stars == null)
                .AsParallel()
                .Select(d => (d.Blob, Stars: SnapshotComparer.StarsOf(setKey, d)))
                .Where(x => x.Stars != null)
                .ToList();

            if (missingStars.Count > 0)
            {
                var starsByBlob = missingStars
                    .GroupBy(x => x.Blob)
                    .ToDictionary(g => g.Key, g => g.First().Stars!.Value);

                SnapshotStore.SaveStars(setKey, starsByBlob);

                foreach (var difficulty in latestDifficulties)
                    if (
                        difficulty.Stars == null
                        && starsByBlob.TryGetValue(difficulty.Blob, out var stars)
                    )
                        difficulty.Stars = stars;
            }

            var result = new List<HistoryEntry>();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var summary = summaries.TryGetValue(entry.Id, out var fresh)
                    ? fresh
                    : entry.Summary!;
                var total = summary.Difficulties.Values.Aggregate(
                    new ChangeCounts(0, 0, 0),
                    (sum, c) =>
                        new ChangeCounts(
                            sum.Added + c.Added,
                            sum.Removed + c.Removed,
                            sum.Changed + c.Changed
                        )
                );

                result.Add(
                    new HistoryEntry(
                        entry.Id,
                        entry.Time,
                        entry.Trigger,
                        entry.Pin,
                        entry.Checks,
                        i > 0 ? entries[i - 1].Checks : null,
                        summary.Difficulties.Keys.ToList(),
                        total,
                        summary.FileChanges,
                        summary.GeneralChanges,
                        i == 0
                    )
                );
            }

            result.Reverse();

            var latest = entries.LastOrDefault();

            return new HistoryResult(
                setKey,
                latest == null
                    ? []
                    : latest
                        .Difficulties.Select(d => new HistoryDifficulty(
                            d.Key,
                            d.Version,
                            d.Mode,
                            d.BeatmapId,
                            d.Stars
                        ))
                        .ToList(),
                result
            );
        }

        private static StoredSummary Summarize(
            string setKey,
            SnapshotEntry before,
            SnapshotEntry after
        )
        {
            var comparison = SnapshotComparer.Compare(setKey, before, after, withStars: false);
            var summary = new StoredSummary
            {
                Version = SummaryVersion,
                FileChanges = comparison.General.Files.Count,
                GeneralChanges =
                    comparison.General.Settings.Count + comparison.General.Rollups.Count,
            };

            foreach (var difficulty in comparison.Difficulties)
            {
                // Changes pulled up into General (a shared offset, tags) belong to the whole set,
                // so a difficulty with nothing of its own is not listed as changed.
                if (
                    difficulty.Status == DifficultyStatus.Unchanged
                    || (
                        difficulty.Status == DifficultyStatus.Changed
                        && difficulty.Counts.Total == 0
                    )
                )
                    continue;

                summary.Difficulties[difficulty.Key] = new StoredCounts
                {
                    Added = difficulty.Counts.Added,
                    Removed = difficulty.Counts.Removed,
                    Changed = Math.Max(difficulty.Counts.Changed, 0),
                };
            }

            return summary;
        }
    }
}
