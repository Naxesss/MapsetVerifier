namespace MapsetVerifier.Snapshots.Store
{
    public sealed class StoredDifficulty
    {
        /// <summary> "b-&lt;beatmapId&gt;" once submitted, otherwise "u-&lt;version&gt;". </summary>
        public string Key { get; set; } = "";
        public ulong? BeatmapId { get; set; }
        public string Version { get; set; } = "";
        public string Mode { get; set; } = "Standard";
        public string File { get; set; } = "";
        public string Blob { get; set; } = "";
        public double? Stars { get; set; }
    }

    public sealed class StoredFile
    {
        public string Hash { get; set; } = "";
        public long Size { get; set; }
    }

    public sealed class StoredChecks
    {
        public int Problems { get; set; }
        public int Warnings { get; set; }
        public int Minor { get; set; }
    }

    public sealed class StoredCounts
    {
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Changed { get; set; }
        public int Total => Added + Removed + Changed;
    }

    /// <summary> What changed since the previous snapshot, kept so the history list needs no diffing. </summary>
    public sealed class StoredSummary
    {
        /// <summary> Counts per difficulty key (only difficulties that changed). </summary>
        public Dictionary<string, StoredCounts> Difficulties { get; set; } = new();
        public int FileChanges { get; set; }
        public int GeneralChanges { get; set; }
    }

    public sealed class SnapshotEntry
    {
        /// <summary> Sortable, unique per set (the capture time to the millisecond). </summary>
        public string Id { get; set; } = "";
        public DateTime Time { get; set; }

        /// <summary> checkRun, pageOpen, manual or import. </summary>
        public string Trigger { get; set; } = "manual";

        /// <summary> A milestone name ("Submitted", "After mod round 2"); null for plain snapshots. </summary>
        public string? Pin { get; set; }
        public List<StoredDifficulty> Difficulties { get; set; } = new();
        public Dictionary<string, StoredFile> Files { get; set; } = new();
        public StoredChecks? Checks { get; set; }
        public StoredSummary? Summary { get; set; }
    }

    public sealed class SetLog
    {
        public List<SnapshotEntry> Entries { get; set; } = new();
    }

    public sealed class StoreIndex
    {
        public int Version { get; set; } = 2;

        /// <summary>
        /// Below <see cref="CurrentMigration" />: imported before long-deleted difficulties were told
        /// apart from current ones, and in need of a rebuild from the old folder.
        /// </summary>
        public int Migration { get; set; }

        public const int CurrentMigration = 2;

        /// <summary> Song folder path to set key, so unsubmitted sets are found again. </summary>
        public Dictionary<string, string> Folders { get; set; } = new();
    }
}
