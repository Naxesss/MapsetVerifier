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
        bool Minor = false
    );

    public enum RollupKind
    {
        /// <summary> Every object and timing point shifted by the same amount of time. </summary>
        Offset,
    }

    /// <summary> One change that explains many others, which are then not listed individually. </summary>
    public sealed record Rollup(RollupKind Kind, double Amount, int Absorbed);

    public sealed record DiffResult(IReadOnlyList<Change> Changes, IReadOnlyList<Rollup> Rollups);

    public enum HunkLabel
    {
        Remapped,
        Rhythm,
        Placement,
        Hitsounding,
        Sv,
        Mixed,
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
