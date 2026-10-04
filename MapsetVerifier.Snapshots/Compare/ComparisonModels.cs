using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;

namespace MapsetVerifier.Snapshots.Compare
{
    /// <summary> A changed setting, metadata field, colour or event; values stay as the .osu has them. </summary>
    public sealed record SettingChange(
        string Section,
        string Key,
        ChangeOp Op,
        string? Before = null,
        string? After = null,
        IReadOnlyList<string>? Added = null,
        IReadOnlyList<string>? Removed = null,
        /// <summary> Set when the same change happened in every difficulty; General shows it once. </summary>
        int AppliesTo = 0
    );

    public sealed record FileChange(
        string Name,
        string Category,
        ChangeOp Op,
        long? SizeBefore,
        long? SizeAfter
    );

    public sealed record TimeRange(double Start, double End);

    /// <summary> A stretch of song time touched by one kind of change, for the change map. </summary>
    public sealed record Mark(ChangeKind Kind, double Start, double End, bool Minor);

    public sealed record ChangeCounts(int Added, int Removed, int Changed)
    {
        public int Total => Added + Removed + Changed;
    }

    /// <summary> What an object looked like, for drawing a hunk before and after. </summary>
    public sealed record VisualObject(
        double Time,
        double? EndTime,
        string Type,
        double X,
        double Y,
        int? Column,
        string HitSound,
        IReadOnlyList<double[]>? Path,
        /// <summary> Where a repeating slider turns around (not its head or tail). </summary>
        IReadOnlyList<double>? Edges = null,
        /// <summary> Catch: the times of the fruits of this object that start a hyperdash. </summary>
        IReadOnlyList<double>? HyperTimes = null,
        /// <summary> osu!: the number the object shows in the editor (1 at each new combo). </summary>
        int? Combo = null,
        /// <summary> osu!: the times of a slider's ticks, following the tick rate (every slide). </summary>
        IReadOnlyList<double>? Ticks = null,
        /// <summary> The volume the object plays at: its own, or the timing line's it inherits. </summary>
        int? Volume = null,
        /// <summary> What an object sounds like: its head, and for a slider each reverse, its tail and the body. </summary>
        IReadOnlyList<SoundPart>? Sounds = null
    );

    /// <summary>
    /// One place a slider makes a sound: Head, Repeat, Tail (with the hit sound flags played there)
    /// or Body (the slide, which only has a whistle).
    /// </summary>
    public sealed record SoundPart(
        double Time,
        string Kind,
        string HitSound,
        /// <summary> The sampleset the sound comes from (Normal, Soft or Drum), as it plays. </summary>
        string Sampleset,
        /// <summary> The sampleset the whistle, finish and clap come from. </summary>
        string Addition
    );

    /// <summary> A red line: where a beat grid starts and how long a beat is. </summary>
    public sealed record TimingMark(double Offset, double BeatLength, int Meter);

    public sealed record HunkVisual(
        IReadOnlyList<VisualObject> Before,
        IReadOnlyList<VisualObject> After,
        bool Truncated,
        /// <summary> The beat grid under the objects, per side (it may differ after a retime). </summary>
        IReadOnlyList<TimingMark> BeforeTiming,
        IReadOnlyList<TimingMark> AfterTiming
    );

    /// <summary>
    /// A fixed stretch of one difficulty (a few beats) around a change, before and after, so every
    /// picture has the same zoom. Where the window sits depends on the change and how far it was
    /// scrolled.
    /// </summary>
    public sealed record WindowResult(
        double From,
        double To,
        /// <summary> How many objects the window shows (of the new version). </summary>
        int Objects,
        bool HasEarlier,
        bool HasLater,
        HunkVisual Visual
    );

    public sealed record HunkResult(
        double Start,
        double End,
        HunkLabel Label,
        IReadOnlyList<ChangeKind> Kinds,
        IReadOnlyList<Change> Changes,
        ChangeCounts Counts
    );

    public enum DifficultyStatus
    {
        Unchanged,
        Changed,
        Added,
        Removed,
    }

    public sealed record DifficultyComparison(
        string Key,
        string Name,
        string Mode,
        ulong? BeatmapId,
        DifficultyStatus Status,
        double? StarsBefore,
        double? StarsAfter,
        int ObjectsBefore,
        int ObjectsAfter,
        ChangeCounts Counts,
        int MinorCount,
        IReadOnlyList<HunkResult> Hunks,
        IReadOnlyList<Change> Minor,
        IReadOnlyList<SettingChange> Settings,
        IReadOnlyList<Rollup> Rollups,
        IReadOnlyList<Mark> Marks,
        IReadOnlyList<TimeRange> Kiai,
        IReadOnlyList<TimeRange> Breaks,
        double LengthMs
    );

    public sealed record GeneralComparison(
        IReadOnlyList<Rollup> Rollups,
        IReadOnlyList<SettingChange> Settings,
        IReadOnlyList<FileChange> Files,
        ChangeCounts Counts
    );

    public sealed record SnapshotInfo(string Id, DateTime Time, string Trigger, string? Pin);

    public sealed record ComparisonResult(
        SnapshotInfo Base,
        SnapshotInfo Target,
        GeneralComparison General,
        IReadOnlyList<DifficultyComparison> Difficulties
    );

    public sealed record HistoryEntry(
        string Id,
        DateTime Time,
        string Trigger,
        string? Pin,
        StoredChecks? Checks,
        StoredChecks? PreviousChecks,
        IReadOnlyList<string> ChangedDifficulties,
        ChangeCounts Counts,
        int FileChanges,
        int GeneralChanges,
        bool IsFirst
    );

    public sealed record HistoryDifficulty(
        string Key,
        string Name,
        string Mode,
        ulong? BeatmapId,
        double? StarRating
    );

    public sealed record HistoryResult(
        string SetKey,
        IReadOnlyList<HistoryDifficulty> Difficulties,
        IReadOnlyList<HistoryEntry> Entries
    );
}
