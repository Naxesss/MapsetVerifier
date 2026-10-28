namespace MapsetVerifier.Snapshots.Diffing
{
    /// <summary>
    /// What a change is about, in the words a mapper uses. The first four are the tracks of
    /// the change map; the rest only occur in General.
    /// </summary>
    public enum ChangeKind
    {
        Rhythm,
        Placement,
        Hitsound,
        Timing,
    }

    public enum ChangeOp
    {
        Added,
        Removed,
        Changed,
    }

    /// <summary>
    /// An object a change is about. <see cref="Stamp" /> is its osu! timestamp including the
    /// object (combo number, or time and column in mania), so it selects exactly that object.
    /// </summary>
    public sealed record ObjectRef(double Time, string Type, string Stamp);

    /// <summary>
    /// A single structured difference between two versions of a difficulty. Values stay typed
    /// where it matters (<see cref="Magnitude" />); the client turns this into a sentence.
    /// A "Shift" is a part of the map moved in time as a whole: its magnitude is the signed
    /// amount and <see cref="Count" /> the objects and timing lines that moved with it.
    /// </summary>
    public sealed record Change(
        ChangeKind Kind,
        ChangeOp Op,
        double Time,
        double? EndTime = null,
        string? Field = null,
        string? Before = null,
        string? After = null,
        double? Magnitude = null,
        ObjectRef? Object = null,
        bool Minor = false,
        int? Count = null
    );

    public enum RollupKind
    {
        /// <summary> Every object and timing point shifted by the same amount of time. </summary>
        Offset,

        /// <summary> Most timing lines' volume changed the same way (by one amount, or to one value). </summary>
        Volume,
    }

    /// <summary>
    /// One change that explains many others, which are then not listed individually. A volume
    /// rollup also names the volume before and after when every line had the same one.
    /// </summary>
    public sealed record Rollup(
        RollupKind Kind,
        double Amount,
        int Absorbed,
        string? Before = null,
        string? After = null
    );

    public sealed record DiffResult(IReadOnlyList<Change> Changes, IReadOnlyList<Rollup> Rollups);

    public enum HunkLabel
    {
        Remapped,
        Rhythm,
        Placement,
        Hitsounding,
        Sv,
        Mixed,
        Shifted,
        Volume,
    }

    /// <summary> Changes that sit close together in the song, shown as one row. </summary>
    public sealed record Hunk(
        double Start,
        double End,
        HunkLabel Label,
        IReadOnlyList<ChangeKind> Kinds,
        IReadOnlyList<Change> Changes
    );
}
