namespace MapsetVerifier.Snapshots.Diffing
{
    /// <summary>
    /// Groups changes that sit close together in the song into hunks and labels each one by what
    /// dominates it. Minor changes are left out; callers fold them into a separate count.
    /// </summary>
    public static class HunkBuilder
    {
        /// <summary> A new hunk starts after this much song time without changes. </summary>
        public const double GapMs = 2000;

        /// <summary> More than this share of the objects in range replaced counts as a remap. </summary>
        private const double RemapShare = 0.6;

        /// <summary>
        /// Changes of the same part of the song are grouped by what kind of work they are, so a
        /// hitsound pass is never mixed into a remap: object changes (rhythm and placement),
        /// hitsounds, and timing each get their own hunks. Hunks of different kinds may overlap
        /// in time. Hunks come back in song order.
        /// </summary>
        public static IReadOnlyList<Hunk> Build(IEnumerable<Change> changes, int objectsInRange = 0)
        {
            var hunks = new List<Hunk>();

            foreach (var group in changes.Where(c => !c.Minor).GroupBy(c => Category(c.Kind)))
                hunks.AddRange(Cluster(group.OrderBy(c => c.Time).ToList()));

            return hunks.OrderBy(h => h.Start).ThenBy(h => h.Kinds[0]).ToList();
        }

        /// <summary> Rhythm and placement are one kind of work: shaping the objects. </summary>
        private static int Category(ChangeKind kind) =>
            kind switch
            {
                ChangeKind.Rhythm or ChangeKind.Placement => 0,
                ChangeKind.Hitsound => 1,
                _ => 2,
            };

        private static List<Hunk> Cluster(List<Change> ordered)
        {
            var hunks = new List<Hunk>();
            var current = new List<Change>();
            var end = double.MinValue;

            foreach (var change in ordered)
            {
                if (current.Count > 0 && change.Time - end > GapMs)
                {
                    hunks.Add(Create(current));
                    current = new List<Change>();
                }

                current.Add(change);
                end = Math.Max(end, change.EndTime ?? change.Time);
            }

            if (current.Count > 0)
                hunks.Add(Create(current));

            return hunks;
        }

        private static Hunk Create(List<Change> changes)
        {
            var kinds = changes.Select(c => c.Kind).Distinct().OrderBy(k => k).ToList();
            var start = changes.Min(c => c.Time);
            var end = changes.Max(c => c.EndTime ?? c.Time);

            return new Hunk(start, end, Label(changes, kinds), kinds, changes);
        }

        private static HunkLabel Label(List<Change> changes, List<ChangeKind> kinds)
        {
            var objectChanges = changes.Where(c => c.Object != null).ToList();
            var replaced = objectChanges.Count(c => c.Op != ChangeOp.Changed);

            if (
                objectChanges.Count > 0
                && replaced >= 3
                && (double)replaced / objectChanges.Count > RemapShare
            )
                return HunkLabel.Remapped;

            if (kinds.Count == 1)
                return kinds[0] switch
                {
                    ChangeKind.Rhythm => HunkLabel.Rhythm,
                    ChangeKind.Placement => HunkLabel.Placement,
                    ChangeKind.Hitsound => HunkLabel.Hitsounding,
                    _ => HunkLabel.Sv,
                };

            return HunkLabel.Mixed;
        }
    }
}
