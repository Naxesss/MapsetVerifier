using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Mania;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Snapshots.Diffing
{
    /// <summary>
    /// Compares two parsed versions of the same difficulty into structured changes: objects and
    /// timing points are paired up (so a move is a move, not a removal plus an addition), and a
    /// uniform time shift is reported once instead of once per object.
    /// </summary>
    public static class BeatmapDiffer
    {
        /// <summary> Retimed objects only pair up when they stay this close in time... </summary>
        private const double RetimeWindowMs = 125;

        /// <summary> ...and in place, so a remapped note is not mistaken for a retimed one. </summary>
        private const double RetimeMaxDistancePx = 40;

        /// <summary> Moves up to this many pixels are minor. </summary>
        private const double MinorMovePx = 3;

        /// <summary> A shift must explain at least this share of the unpaired objects to count. </summary>
        private const double OffsetShare = 0.6;

        private const int OffsetMinimumObjects = 3;

        private const double MaxShiftMs = 1000;

        /// <summary>
        /// Objects moved by an offset change land within this much of the shift, not exactly on
        /// it: times are whole milliseconds and the editor rounds each one on its own.
        /// </summary>
        private const double ShiftToleranceMs = 1.5;

        /// <summary> How many separately moved groups of objects are looked for at most. </summary>
        private const int MaxShiftGroups = 32;

        /// <summary> Looking for groups stops after this many amounts in a row explained none. </summary>
        private const int MaxFruitlessShifts = 4;

        /// <summary>
        /// A group moved further than a retime needs more objects, so a short pattern that happens
        /// to sit in the same place elsewhere in a remapped part is not taken for a moved one.
        /// </summary>
        private const int FarShiftMinimumObjects = 6;

        /// <summary>
        /// Objects that moved in time together, by one amount, with nothing left in place between
        /// them: an offset change when it is most of the map, otherwise a part of it moved on its
        /// own (a section moved with its timing lines, say).
        /// </summary>
        private sealed class ShiftGroup
        {
            public required double Delta { get; init; }
            public required List<(HitObject Old, HitObject New)> Pairs { get; init; }
            public int FirstIndex { get; init; }
            public int LastIndex { get; init; }

            /// <summary> Old times of the nearest objects outside the group; its lines lie between. </summary>
            public double From { get; set; } = double.NegativeInfinity;
            public double To { get; set; } = double.PositiveInfinity;
            public List<(TimingLine Old, TimingLine New)> Lines { get; } = [];

            /// <summary> Part of a shift of the whole map, reported as one rollup. </summary>
            public bool Whole { get; set; }
        }

        public static DiffResult Diff(Beatmap before, Beatmap after)
        {
            var changes = new List<Change>();
            var rollups = new List<Rollup>();

            var objectPairs = MatchObjects(
                before,
                after,
                out var oldOnly,
                out var newOnly,
                out var groups
            );

            foreach (var (oldObject, newObject, delta) in objectPairs)
                changes.AddRange(DiffObject(before, after, oldObject, newObject, delta));

            foreach (var o in oldOnly)
                changes.Add(ObjectChange(after, o, ChangeOp.Removed));

            foreach (var o in newOnly)
                changes.Add(ObjectChange(after, o, ChangeOp.Added));

            DiffTiming(before, after, groups, changes);

            var whole = groups.Where(g => g.Whole).ToList();
            var shift = whole.Count == 0 ? 0 : whole.MaxBy(g => g.Pairs.Count)!.Delta;

            if (whole.Count > 0)
                rollups.Add(
                    new Rollup(
                        RollupKind.Offset,
                        shift,
                        whole.Sum(g => g.Pairs.Count + g.Lines.Count)
                    )
                );

            foreach (var group in groups.Where(g => !g.Whole))
                changes.Add(ShiftChange(group, shift));

            GroupVolumeChanges(after, changes, rollups);

            return new DiffResult(changes, rollups);
        }

        // ---------- Volume passes ----------

        private static readonly string[] VolumeFields = ["RedLineVolume", "GreenLineVolume"];

        /// <summary>
        /// Timing lines next to each other whose volume changed the same way (by the same amount,
        /// or all to the same value) are one change, like objects moved together: a rollup when it
        /// is most of the map's lines, otherwise one change for that stretch. A line whose volume
        /// stayed ends a stretch; a new line has no old volume to compare, so it does not.
        /// </summary>
        private static void GroupVolumeChanges(
            Beatmap after,
            List<Change> changes,
            List<Rollup> rollups
        )
        {
            var volume = changes
                .Where(c => c.Op == ChangeOp.Changed && VolumeFields.Contains(c.Field))
                .GroupBy(c => (c.Time, c.Field == "RedLineVolume"))
                .ToDictionary(g => g.Key, g => g.First());

            if (volume.Count < OffsetMinimumObjects)
                return;

            var added = changes
                .Where(c => c.Op == ChangeOp.Added && c.Field is "RedLine" or "GreenLine")
                .Select(c => (c.Time, c.Field == "RedLine"))
                .ToHashSet();

            var runs = new List<List<Change>>();
            var run = new List<Change>();

            void Close()
            {
                if (run.Count > 0)
                    runs.Add(run);
                run = [];
            }

            foreach (var line in after.TimingLines.OrderBy(l => l.Offset))
            {
                if (added.Contains((line.Offset, line.Uninherited)))
                    continue;

                if (!volume.TryGetValue((line.Offset, line.Uninherited), out var change))
                {
                    Close();
                    continue;
                }

                if (run.Count > 0 && !Alike([.. run, change]))
                    Close();

                run.Add(change);
            }

            Close();

            foreach (var group in runs.Where(r => r.Count >= OffsetMinimumObjects))
            {
                changes.RemoveAll(group.Contains);

                var befores = group.Select(c => c.Before).Distinct().ToList();
                var afters = group.Select(c => c.After).Distinct().ToList();
                var deltas = group.Select(VolumeDelta).Distinct().ToList();
                var before = befores.Count == 1 ? befores[0] : null;
                var afterValue = afters.Count == 1 ? afters[0] : null;
                var delta = deltas.Count == 1 ? deltas[0] : (double?)null;

                if (group.Count >= after.TimingLines.Count * OffsetShare)
                    rollups.Add(
                        new Rollup(RollupKind.Volume, delta ?? 0, group.Count, before, afterValue)
                    );
                else
                    changes.Add(
                        new Change(
                            ChangeKind.Hitsound,
                            ChangeOp.Changed,
                            group[0].Time,
                            EndTime: group[^1].Time,
                            Field: "VolumeGroup",
                            Before: before,
                            After: afterValue,
                            Magnitude: delta,
                            Count: group.Count
                        )
                    );
            }
        }

        private static double VolumeDelta(Change c) =>
            double.Parse(c.After!, CultureInfo.InvariantCulture)
            - double.Parse(c.Before!, CultureInfo.InvariantCulture);

        /// <summary> Whether volume changes went the same way: by one amount, or to one value. </summary>
        private static bool Alike(List<Change> group) =>
            group.Select(VolumeDelta).Distinct().Count() == 1
            || group.Select(c => c.After).Distinct().Count() == 1;

        /// <summary> One change for a part of the map that moved in time as a whole. </summary>
        private static Change ShiftChange(ShiftGroup group, double wholeShift)
        {
            var oldStart = group
                .Pairs.Select(p => p.Old.time)
                .Concat(group.Lines.Select(l => l.Old.Offset))
                .Min();
            var newStart = group
                .Pairs.Select(p => p.New.time)
                .Concat(group.Lines.Select(l => l.New.Offset))
                .Min();
            var newEnd = group
                .Pairs.Select(p => p.New.GetEndTime())
                .Concat(group.Lines.Select(l => l.New.Offset))
                .Max();

            return new Change(
                ChangeKind.Timing,
                ChangeOp.Changed,
                newStart,
                EndTime: newEnd,
                Field: "Shift",
                Before: Format(oldStart),
                After: Format(newStart),
                // Signed, and on top of an offset change of the whole map if there was one.
                Magnitude: group.Delta - wholeShift,
                Count: group.Pairs.Count + group.Lines.Count
            );
        }

        // ---------- Objects ----------

        /// <summary>
        /// Pairs old objects with new ones. Each pair carries the shift that explains its move in
        /// time (zero without one), so only what changed on top of that is reported.
        /// </summary>
        private static List<(HitObject Old, HitObject New, double Delta)> MatchObjects(
            Beatmap before,
            Beatmap after,
            out List<HitObject> oldOnly,
            out List<HitObject> newOnly,
            out List<ShiftGroup> groups
        )
        {
            var pairs = new List<(HitObject, HitObject, double)>();
            var all = before.HitObjects.OrderBy(o => o.time).ToList();
            var oldLeft = all.ToList();
            var newLeft = after.HitObjects.OrderBy(o => o.time).ToList();

            // Pass 1: same time and type. Chords and mania columns pair by the closest position.
            PairWhere(oldLeft, newLeft, pairs, 0, (o, n) => AlmostEqual(o.time, n.time));

            // Pass 2: runs of objects that all moved by the same amount, kept in place otherwise.
            var inPlace = pairs.Select(p => p.Item1).ToHashSet();
            groups = FindShiftGroups(all, inPlace, oldLeft, newLeft);

            foreach (var group in groups)
                pairs.AddRange(group.Pairs.Select(p => (p.Old, p.New, group.Delta)));

            MarkWholeShift(groups, all.Count);
            SetGroupBounds(groups, all, inPlace);

            // Pass 3: small retimes in place, on top of an offset change if there is one.
            var whole = groups.Where(g => g.Whole).ToList();
            var delta = whole.Count == 0 ? 0 : whole.MaxBy(g => g.Pairs.Count)!.Delta;

            PairWhere(
                oldLeft,
                newLeft,
                pairs,
                delta,
                (o, n) =>
                    Math.Abs(o.time + delta - n.time) <= RetimeWindowMs
                    && Vector2Distance(o, n) <= RetimeMaxDistancePx
            );

            // Pass 4: an object turned into another kind in place (a hold note into a note, a
            // circle into a slider) is that object changed, not one removed and another added.
            // In place means the same column in mania, anywhere in taiko (where a note sits does not
            // matter there) and close by otherwise.
            var mode = after.GeneralSettings.mode;
            var keys = after.DifficultySettings.circleSize;

            PairWhere(
                oldLeft,
                newLeft,
                pairs,
                delta,
                (o, n) =>
                    AlmostEqual(o.time + delta, n.time)
                    && mode switch
                    {
                        Beatmap.Mode.Mania => ManiaExtensions.GetColumn(o, keys)
                            == ManiaExtensions.GetColumn(n, keys),
                        Beatmap.Mode.Taiko => true,
                        _ => Vector2Distance(o, n) <= RetimeMaxDistancePx,
                    },
                anyType: true
            );

            oldOnly = oldLeft;
            newOnly = newLeft;

            return pairs;
        }

        /// <summary>
        /// Greedily pairs objects of the same type that satisfy <paramref name="match" />, closest
        /// in position first. <paramref name="delta" /> is the shift the pairs are expected to share.
        /// With <paramref name="anyType" /> the type may differ too.
        /// </summary>
        private static void PairWhere(
            List<HitObject> oldLeft,
            List<HitObject> newLeft,
            List<(HitObject, HitObject, double)> pairs,
            double delta,
            Func<HitObject, HitObject, bool> match,
            bool anyType = false
        )
        {
            var candidates = new List<(HitObject Old, HitObject New, double Cost)>();
            var window = RetimeWindowMs + 1;
            var newTimes = newLeft.Select(n => n.time).ToArray();

            foreach (var o in oldLeft)
            {
                var type = o.GetObjectType();

                // Both lists are sorted by time, so only a window around o can match.
                for (var i = LowerBound(newTimes, o.time + delta - window); i < newLeft.Count; i++)
                {
                    var n = newLeft[i];

                    if (n.time > o.time + delta + window)
                        break;

                    if ((!anyType && n.GetObjectType() != type) || !match(o, n))
                        continue;

                    candidates.Add(
                        (o, n, Vector2Distance(o, n) + Math.Abs(o.time + delta - n.time) / 10)
                    );
                }
            }

            var usedOld = new HashSet<HitObject>();
            var usedNew = new HashSet<HitObject>();

            foreach (var (o, n, _) in candidates.OrderBy(c => c.Cost))
            {
                if (usedOld.Contains(o) || usedNew.Contains(n))
                    continue;

                usedOld.Add(o);
                usedNew.Add(n);
                pairs.Add((o, n, delta));
            }

            oldLeft.RemoveAll(usedOld.Contains);
            newLeft.RemoveAll(usedNew.Contains);
        }

        /// <summary>
        /// Finds the groups of objects that moved in time together. The amount most unpaired
        /// objects agree on is tried first; the objects it explains are split into runs wherever
        /// an object stayed in place between them, so two parts moved by the same amount stay two
        /// groups. Then the next amount, until no amount explains a run.
        /// </summary>
        private static List<ShiftGroup> FindShiftGroups(
            List<HitObject> all,
            HashSet<HitObject> inPlace,
            List<HitObject> oldLeft,
            List<HitObject> newLeft
        )
        {
            var groups = new List<ShiftGroup>();
            var tried = new List<double>();
            var index = all.Select((o, i) => (o, i)).ToDictionary(x => x.o, x => x.i);

            // Objects whose place is settled: left in place, or moved with a group found earlier.
            var settled = inPlace.ToHashSet();

            var fruitless = 0;

            while (groups.Count < MaxShiftGroups && fruitless < MaxFruitlessShifts)
            {
                var delta = DensestShift(oldLeft, newLeft, tried);

                if (delta == null)
                    break;

                tried.Add(delta.Value);
                var d = delta.Value;
                var found = groups.Count;

                // Votes either side of nothing can meet in the middle; that is no shift at all.
                if (d == 0)
                    continue;

                var minimum =
                    Math.Abs(d) > RetimeWindowMs ? FarShiftMinimumObjects : OffsetMinimumObjects;
                var newTimes = newLeft.Select(n => n.time).ToArray();

                var explained = oldLeft
                    .Where(o =>
                        newLeft
                            .Skip(LowerBound(newTimes, o.time + d - ShiftToleranceMs))
                            .TakeWhile(n => n.time <= o.time + d + ShiftToleranceMs)
                            .Any(n =>
                                n.GetObjectType() == o.GetObjectType()
                                && Vector2Distance(o, n) <= RetimeMaxDistancePx
                            )
                    )
                    .ToList();

                foreach (var run in Runs(explained, all, index, settled).ToList())
                {
                    // Most of what is unsettled in the run has to have moved, so a few notes that
                    // happen to line up in a remapped part do not make a group.
                    var first = index[run[0]];
                    var last = index[run[^1]];
                    var unsettled = all.Skip(first)
                        .Take(last - first + 1)
                        .Count(o => !settled.Contains(o));

                    if (run.Count < minimum || run.Count < unsettled * OffsetShare)
                        continue;

                    var runLeft = run.ToList();
                    var pairs = new List<(HitObject, HitObject, double)>();
                    PairWhere(
                        runLeft,
                        newLeft,
                        pairs,
                        d,
                        (o, n) => Math.Abs(o.time + d - n.time) <= ShiftToleranceMs
                    );
                    var moved = pairs.Select(p => p.Item1).ToHashSet();
                    oldLeft.RemoveAll(moved.Contains);
                    settled.UnionWith(moved);

                    groups.Add(
                        new ShiftGroup
                        {
                            Delta = d,
                            Pairs = pairs.Select(p => (p.Item1, p.Item2)).ToList(),
                            FirstIndex = first,
                            LastIndex = last,
                        }
                    );
                }

                fruitless = groups.Count > found ? 0 : fruitless + 1;
            }

            return groups.OrderBy(g => g.FirstIndex).ToList();
        }

        /// <summary>
        /// Splits objects into runs that follow each other in the map with no settled object
        /// between them. Unsettled ones (remapped, say) do not break a run.
        /// </summary>
        private static IEnumerable<List<HitObject>> Runs(
            List<HitObject> objects,
            List<HitObject> all,
            Dictionary<HitObject, int> index,
            HashSet<HitObject> settled
        )
        {
            var run = new List<HitObject>();

            foreach (var o in objects.OrderBy(o => index[o]))
            {
                if (run.Count > 0)
                {
                    var from = index[run[^1]] + 1;
                    var to = index[o];

                    if (all.Skip(from).Take(to - from).Any(settled.Contains))
                    {
                        yield return run;
                        run = [];
                    }
                }

                run.Add(o);
            }

            if (run.Count > 0)
                yield return run;
        }

        /// <summary>
        /// The time shift the most unpaired objects agree on, other than those already tried.
        /// Every object votes for each object of its type nearby in place; agreeing means landing
        /// within <see cref="ShiftToleranceMs" /> of each other, and the shift is the middle of
        /// that group.
        /// </summary>
        private static double? DensestShift(
            List<HitObject> oldLeft,
            List<HitObject> newLeft,
            List<double> tried
        )
        {
            var deltas = new List<double>();
            var newTimes = newLeft.Select(n => n.time).ToArray();

            foreach (var o in oldLeft)
            {
                var type = o.GetObjectType();

                for (var i = LowerBound(newTimes, o.time - MaxShiftMs); i < newLeft.Count; i++)
                {
                    var n = newLeft[i];

                    if (n.time > o.time + MaxShiftMs)
                        break;

                    // A shifted object stays roughly where it was; a remapped one does not vote.
                    if (n.GetObjectType() != type || Vector2Distance(o, n) > RetimeMaxDistancePx)
                        continue;

                    var delta = n.time - o.time;

                    if (
                        Math.Abs(delta) >= 0.5
                        && !tried.Any(t => Math.Abs(t - delta) <= ShiftToleranceMs)
                    )
                        deltas.Add(delta);
                }
            }

            // The densest stretch of votes no wider than the tolerance either way.
            deltas.Sort();
            int bestStart = 0,
                bestCount = 0;

            for (int start = 0, end = 0; start < deltas.Count; start++)
            {
                while (end < deltas.Count && deltas[end] - deltas[start] <= 2 * ShiftToleranceMs)
                    end++;

                if (end - start > bestCount)
                {
                    bestStart = start;
                    bestCount = end - start;
                }
            }

            if (bestCount < OffsetMinimumObjects)
                return null;

            var group = deltas.GetRange(bestStart, bestCount);
            var median =
                group.Count % 2 == 1
                    ? group[group.Count / 2]
                    : (group[group.Count / 2 - 1] + group[group.Count / 2]) / 2;

            return Math.Round(median * 2) / 2;
        }

        /// <summary>
        /// Groups that moved by one amount and together hold most of the map are an offset change
        /// of the whole map, not parts of it moved one by one.
        /// </summary>
        private static void MarkWholeShift(List<ShiftGroup> groups, int objectCount)
        {
            foreach (var group in groups.OrderByDescending(g => g.Pairs.Count))
            {
                var alike = groups
                    .Where(g => Math.Abs(g.Delta - group.Delta) <= ShiftToleranceMs)
                    .ToList();

                if (alike.Sum(g => g.Pairs.Count) < objectCount * OffsetShare)
                    continue;

                foreach (var g in alike)
                    g.Whole = true;

                return;
            }
        }

        /// <summary>
        /// Each group reaches to the nearest object outside it that stayed in place or moved with
        /// another group, so the timing lines between those are the group's to move.
        /// </summary>
        private static void SetGroupBounds(
            List<ShiftGroup> groups,
            List<HitObject> all,
            HashSet<HitObject> inPlace
        )
        {
            var owner = new Dictionary<HitObject, ShiftGroup>();

            foreach (var group in groups)
            foreach (var (o, _) in group.Pairs)
                owner[o] = group;

            bool Bounds(HitObject o, ShiftGroup group) =>
                inPlace.Contains(o) || (owner.TryGetValue(o, out var other) && other != group);

            foreach (var group in groups)
            {
                var before = all.Take(group.FirstIndex).LastOrDefault(o => Bounds(o, group));
                var after = all.Skip(group.LastIndex + 1).FirstOrDefault(o => Bounds(o, group));

                group.From = before?.time ?? double.NegativeInfinity;
                group.To = after?.time ?? double.PositiveInfinity;
            }
        }

        private static IEnumerable<Change> DiffObject(
            Beatmap before,
            Beatmap after,
            HitObject o,
            HitObject n,
            double delta
        )
        {
            var reference = ObjectRef(after, n);
            var type = n.GetObjectType();

            // A shift shared with the rollup is not a change of its own; anything on top of it is,
            // past the rounding the shift brings with it.
            var retimed = n.time - o.time - delta;

            if (!WithinShift(retimed, delta))
                yield return new Change(
                    ChangeKind.Rhythm,
                    ChangeOp.Changed,
                    n.time,
                    Field: "Time",
                    Before: Format(o.time),
                    After: Format(n.time),
                    Magnitude: Math.Abs(retimed),
                    Object: reference
                );

            var distance = Vector2Distance(o, n);

            if (after.GeneralSettings.mode == Beatmap.Mode.Mania)
            {
                var keys = after.DifficultySettings.circleSize;
                var oldColumn = ManiaExtensions.GetColumn(o, keys);
                var newColumn = ManiaExtensions.GetColumn(n, keys);

                if (oldColumn != newColumn)
                    yield return new Change(
                        ChangeKind.Placement,
                        ChangeOp.Changed,
                        n.time,
                        Field: "Column",
                        Before: (oldColumn + 1).ToString(CultureInfo.InvariantCulture),
                        After: (newColumn + 1).ToString(CultureInfo.InvariantCulture),
                        Object: reference
                    );
            }
            // Where a note sits on the playfield does not matter in taiko, only when and what it is.
            else if (distance > 0.001 && after.GeneralSettings.mode != Beatmap.Mode.Taiko)
            {
                yield return new Change(
                    ChangeKind.Placement,
                    ChangeOp.Changed,
                    n.time,
                    Field: "Position",
                    Before: o.Position.X + "," + o.Position.Y,
                    After: n.Position.X + "," + n.Position.Y,
                    Magnitude: Math.Round(distance, 1),
                    Object: reference,
                    Minor: distance <= MinorMovePx
                );
            }

            var wasNewCombo = o.type.HasFlag(HitObject.Types.NewCombo);
            var isNewCombo = n.type.HasFlag(HitObject.Types.NewCombo);

            // Mania and taiko have no combo colours or numbers, so a new combo does nothing there.
            if (
                wasNewCombo != isNewCombo
                && after.GeneralSettings.mode is not (Beatmap.Mode.Mania or Beatmap.Mode.Taiko)
            )
                yield return new Change(
                    ChangeKind.Placement,
                    ChangeOp.Changed,
                    n.time,
                    Field: "NewCombo",
                    Before: wasNewCombo.ToString(),
                    After: isNewCombo.ToString(),
                    Object: reference
                );

            if (Plays(o.hitSound) != Plays(n.hitSound))
            {
                // In taiko the hit sounds are what a note looks like (don, kat, big), so changing
                // them changes the note itself, like moving it does in the other modes.
                var oldNote =
                    after.GeneralSettings.mode == Beatmap.Mode.Taiko ? TaikoNote(o) : null;
                var newNote =
                    after.GeneralSettings.mode == Beatmap.Mode.Taiko ? TaikoNote(n) : null;

                if (oldNote != newNote)
                    yield return new Change(
                        ChangeKind.Placement,
                        ChangeOp.Changed,
                        n.time,
                        Field: "Note",
                        Before: oldNote,
                        After: newNote,
                        Object: reference
                    );
                else
                    yield return new Change(
                        ChangeKind.Hitsound,
                        ChangeOp.Changed,
                        n.time,
                        Field: "Hitsound",
                        Before: o.hitSound.ToString(),
                        After: n.hitSound.ToString(),
                        Object: reference
                    );
            }

            // A sampleset set by hand to what the timing line already gives plays the same, so only a
            // change in what actually plays counts.
            if (
                (o.sampleset != n.sampleset || o.addition != n.addition)
                && (
                    o.GetSampleset() != n.GetSampleset()
                    || o.GetSampleset(true) != n.GetSampleset(true)
                )
            )
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.time,
                    Field: "Sampleset",
                    Before: o.sampleset + "/" + o.addition,
                    After: n.sampleset + "/" + n.addition,
                    Object: reference
                );

            if ((o.customIndex ?? 0) != (n.customIndex ?? 0))
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.time,
                    Field: "CustomIndex",
                    Before: (o.customIndex ?? 0).ToString(CultureInfo.InvariantCulture),
                    After: (n.customIndex ?? 0).ToString(CultureInfo.InvariantCulture),
                    Object: reference
                );

            // The same goes for a volume typed in that equals the one it inherits.
            if (o.volume != n.volume && EffectiveVolume(o) != EffectiveVolume(n))
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.time,
                    Field: "Volume",
                    Before: o.volume?.ToString(CultureInfo.InvariantCulture),
                    After: n.volume?.ToString(CultureInfo.InvariantCulture),
                    Magnitude: Math.Abs((n.volume ?? 0) - (o.volume ?? 0)),
                    Object: reference,
                    Minor: o.volume != null
                        && n.volume != null
                        && Math.Abs(n.volume.Value - o.volume.Value) <= 3
                );

            if (o.filename != n.filename)
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.time,
                    Field: "Filename",
                    Before: o.filename,
                    After: n.filename,
                    Object: reference
                );

            if (o.GetObjectType() != type)
                yield return new Change(
                    ChangeKind.Rhythm,
                    ChangeOp.Changed,
                    n.time,
                    EndTime: n.GetEndTime(),
                    Field: "Type",
                    Before: KindName(before, o),
                    After: KindName(after, n),
                    Object: reference
                );
            else if (type == "Slider")
                foreach (var change in DiffSlider(before, after, o, n, reference))
                    yield return change;
            else if (type is "Spinner" or "Hold note")
            {
                var oldEnd = o.GetEndTime() + delta;
                var newEnd = n.GetEndTime();

                if (!WithinShift(newEnd - oldEnd, delta))
                    yield return new Change(
                        ChangeKind.Rhythm,
                        ChangeOp.Changed,
                        n.time,
                        EndTime: newEnd,
                        Field: "EndTime",
                        Before: Format(oldEnd),
                        After: Format(newEnd),
                        Magnitude: Math.Abs(newEnd - oldEnd),
                        Object: reference
                    );
            }
        }

        /// <summary> What a kind of object is called in its mode: a note in mania, a drumroll in taiko. </summary>
        private static string KindName(Beatmap map, HitObject o) =>
            map.GeneralSettings.mode switch
            {
                Beatmap.Mode.Taiko => TaikoNote(o),
                Beatmap.Mode.Mania => o.GetObjectType() == "Hold note" ? "Hold note" : "Note",
                _ => o.GetObjectType(),
            };

        /// <summary> What a taiko note is called in game: don, kat, a roll or a shaker, and whether it is big. </summary>
        private static string TaikoNote(HitObject o)
        {
            var big = o.hitSound.HasFlag(HitObject.HitSounds.Finish);
            var type = o.GetObjectType();

            if (type == "Spinner")
                return "Shaker";

            if (type == "Slider")
                return big ? "Big drumroll" : "Drumroll";

            var kat =
                o.hitSound.HasFlag(HitObject.HitSounds.Whistle)
                || o.hitSound.HasFlag(HitObject.HitSounds.Clap);

            return big ? (kat ? "Big kat" : "Big don") : (kat ? "Kat" : "Don");
        }

        private static IEnumerable<Change> DiffSlider(
            Beatmap before,
            Beatmap after,
            HitObject o,
            HitObject n,
            ObjectRef reference
        )
        {
            var oldSlider = new Slider(o.code.Split(','), before);
            var newSlider = new Slider(n.code.Split(','), after);

            if (!oldSlider.PixelLength.AlmostEqual(newSlider.PixelLength))
                yield return new Change(
                    ChangeKind.Rhythm,
                    ChangeOp.Changed,
                    n.time,
                    EndTime: newSlider.EndTime,
                    Field: "Length",
                    Before: Format(oldSlider.PixelLength),
                    After: Format(newSlider.PixelLength),
                    Magnitude: Math.Abs(newSlider.PixelLength - oldSlider.PixelLength),
                    Object: reference
                );

            if (oldSlider.EdgeAmount != newSlider.EdgeAmount)
                yield return new Change(
                    ChangeKind.Rhythm,
                    ChangeOp.Changed,
                    n.time,
                    EndTime: newSlider.EndTime,
                    Field: "Reverses",
                    Before: (oldSlider.EdgeAmount - 1).ToString(CultureInfo.InvariantCulture),
                    After: (newSlider.EdgeAmount - 1).ToString(CultureInfo.InvariantCulture),
                    Object: reference
                );

            var shapeChanged =
                oldSlider.CurveType != newSlider.CurveType
                || oldSlider.NodePositions.Count != newSlider.NodePositions.Count
                || oldSlider
                    .NodePositions.Skip(1)
                    .Zip(newSlider.NodePositions.Skip(1))
                    .Any(pair => pair.First != pair.Second);

            // A drumroll's curve is never seen in taiko; only its length (above) is played.
            if (shapeChanged && after.GeneralSettings.mode != Beatmap.Mode.Taiko)
                yield return new Change(
                    ChangeKind.Placement,
                    ChangeOp.Changed,
                    n.time,
                    EndTime: newSlider.EndTime,
                    Field: "Shape",
                    Object: reference
                );

            var samplesChanged =
                oldSlider.StartHitSound != newSlider.StartHitSound
                || oldSlider.EndHitSound != newSlider.EndHitSound
                || oldSlider.StartSampleset != newSlider.StartSampleset
                || oldSlider.EndSampleset != newSlider.EndSampleset
                || oldSlider.StartAddition != newSlider.StartAddition
                || oldSlider.EndAddition != newSlider.EndAddition
                || !oldSlider.ReverseHitSounds.SequenceEqual(newSlider.ReverseHitSounds)
                || !oldSlider.ReverseSamplesets.SequenceEqual(newSlider.ReverseSamplesets)
                || !oldSlider.ReverseAdditions.SequenceEqual(newSlider.ReverseAdditions);

            // Only when something plays differently, not when a value was set to what it inherited.
            if (samplesChanged && SlidersSoundDifferent(oldSlider, newSlider))
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.time,
                    EndTime: newSlider.EndTime,
                    Field: "SliderSamples",
                    Object: reference
                );
        }

        /// <summary> No flags at all plays the normal sound, so "none" and "normal" sound the same. </summary>
        private static HitObject.HitSounds Plays(HitObject.HitSounds sound) =>
            sound == HitObject.HitSounds.None ? HitObject.HitSounds.Normal : sound;

        private static double EffectiveVolume(HitObject o) =>
            o.volume ?? o.beatmap.GetTimingLine(o.time, true)?.Volume ?? 0;

        /// <summary> Whether two versions of a slider play different sounds at its head, reverses or tail. </summary>
        private static bool SlidersSoundDifferent(Slider a, Slider b)
        {
            if (
                Plays(a.StartHitSound) != Plays(b.StartHitSound)
                || Plays(a.EndHitSound) != Plays(b.EndHitSound)
            )
                return true;

            // A slider line may leave its sound fields out, which means the same as zeros. With a
            // different number of reverses only the ones both versions have can be compared; the
            // missing ones are part of the reverses change itself.
            var shared = Math.Min(a.EdgeAmount, b.EdgeAmount) - 1;
            for (var i = 0; i < shared; i++)
                if (
                    Plays(a.ReverseHitSounds.ElementAtOrDefault(i))
                    != Plays(b.ReverseHitSounds.ElementAtOrDefault(i))
                )
                    return true;

            foreach (var additions in new[] { false, true })
            {
                if (
                    a.GetStartSampleset(additions) != b.GetStartSampleset(additions)
                    || a.GetEndSampleset(additions) != b.GetEndSampleset(additions)
                )
                    return true;

                for (var i = 0; i < shared; i++)
                    if (a.GetReverseSampleset(i, additions) != b.GetReverseSampleset(i, additions))
                        return true;
            }

            return false;
        }

        private static Change ObjectChange(Beatmap map, HitObject o, ChangeOp op) =>
            new(
                ChangeKind.Rhythm,
                op,
                o.time,
                EndTime: o.GetEndTime(),
                Field: "Object",
                Object: ObjectRef(map, o)
            );

        private static ObjectRef ObjectRef(Beatmap map, HitObject o)
        {
            string stamp;

            try
            {
                stamp = Timestamp.Get(o);
            }
            catch
            {
                // A stamp without the object still jumps to the right time.
                stamp = Timestamp.Get(o.time);
            }

            return new ObjectRef(o.time, o.GetObjectType(), stamp);
        }

        // ---------- Timing ----------

        /// <summary>
        /// Diffs timing lines. A line that moved with a group of objects, between the objects
        /// around that group, is part of the group rather than a change of its own.
        /// </summary>
        private static void DiffTiming(
            Beatmap before,
            Beatmap after,
            List<ShiftGroup> groups,
            List<Change> changes
        )
        {
            var oldLeft = before.TimingLines.ToList();
            var newLeft = after.TimingLines.ToList();

            void Pair(double delta, ShiftGroup? group)
            {
                foreach (var o in oldLeft.ToList())
                {
                    if (group != null && (o.Offset <= group.From || o.Offset >= group.To))
                        continue;

                    var n = newLeft.FirstOrDefault(l =>
                        l.Uninherited == o.Uninherited
                        && (
                            delta == 0
                                ? AlmostEqual(o.Offset, l.Offset)
                                : Math.Abs(o.Offset + delta - l.Offset) <= ShiftToleranceMs
                        )
                    );

                    if (n == null)
                        continue;

                    oldLeft.Remove(o);
                    newLeft.Remove(n);
                    group?.Lines.Add((o, n));

                    changes.AddRange(DiffLine(o, n));
                }
            }

            Pair(0, null);

            foreach (var group in groups)
                Pair(group.Delta, group);

            foreach (var l in oldLeft)
                changes.Add(LineChange(l, ChangeOp.Removed));

            foreach (var l in newLeft)
                changes.Add(LineChange(l, ChangeOp.Added));
        }

        /// <summary>
        /// A timing line added or removed. A green line names its slider velocity, on the side it
        /// exists on, so it can be shown with the other SV changes.
        /// </summary>
        private static Change LineChange(TimingLine line, ChangeOp op)
        {
            var sv = line.Uninherited ? null : Format(line.SvMult);

            return new(
                ChangeKind.Timing,
                op,
                line.Offset,
                Field: line.Uninherited ? "RedLine" : "GreenLine",
                Before: op == ChangeOp.Removed ? sv : null,
                After: op == ChangeOp.Added ? sv : null
            );
        }

        private static IEnumerable<Change> DiffLine(TimingLine o, TimingLine n)
        {
            var field = n.Uninherited ? "RedLine" : "GreenLine";

            if (o.Kiai != n.Kiai)
                yield return new Change(
                    ChangeKind.Timing,
                    ChangeOp.Changed,
                    n.Offset,
                    Field: "Kiai",
                    Before: o.Kiai.ToString(),
                    After: n.Kiai.ToString()
                );

            if (o.Meter != n.Meter)
                yield return new Change(
                    ChangeKind.Timing,
                    ChangeOp.Changed,
                    n.Offset,
                    Field: "Meter",
                    Before: o.Meter.ToString(CultureInfo.InvariantCulture),
                    After: n.Meter.ToString(CultureInfo.InvariantCulture)
                );

            if (o.Sampleset != n.Sampleset || o.CustomIndex != n.CustomIndex)
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.Offset,
                    Field: field + "Sampleset",
                    Before: o.Sampleset + "/" + o.CustomIndex,
                    After: n.Sampleset + "/" + n.CustomIndex
                );

            if (!o.Volume.AlmostEqual(n.Volume))
                yield return new Change(
                    ChangeKind.Hitsound,
                    ChangeOp.Changed,
                    n.Offset,
                    Field: field + "Volume",
                    Before: Format(o.Volume),
                    After: Format(n.Volume),
                    Magnitude: Math.Abs(n.Volume - o.Volume),
                    Minor: Math.Abs(n.Volume - o.Volume) <= 3
                );

            if (n.Uninherited)
            {
                var oldBpm = Bpm(o);
                var newBpm = Bpm(n);

                if (!oldBpm.AlmostEqual(newBpm, 1e-6))
                    yield return new Change(
                        ChangeKind.Timing,
                        ChangeOp.Changed,
                        n.Offset,
                        Field: "Bpm",
                        Before: Format(oldBpm),
                        After: Format(newBpm),
                        Magnitude: Math.Abs(newBpm - oldBpm)
                    );
            }
            else if (!o.SvMult.AlmostEqual(n.SvMult))
            {
                yield return new Change(
                    ChangeKind.Timing,
                    ChangeOp.Changed,
                    n.Offset,
                    Field: "Sv",
                    Before: Format(o.SvMult),
                    After: Format(n.SvMult),
                    Magnitude: Math.Abs(n.SvMult - o.SvMult),
                    Minor: Math.Abs(n.SvMult - o.SvMult) <= 0.02
                );
            }
        }

        private static double Bpm(TimingLine line) =>
            60000 / double.Parse(line.Code.Split(',')[1], CultureInfo.InvariantCulture);

        // ---------- Helpers ----------

        /// <summary> Index of the first time that is not below <paramref name="value" />. </summary>
        private static int LowerBound(double[] sortedTimes, double value)
        {
            int low = 0,
                high = sortedTimes.Length;

            while (low < high)
            {
                var mid = (low + high) / 2;

                if (sortedTimes[mid] < value)
                    low = mid + 1;
                else
                    high = mid;
            }

            return low;
        }

        private static bool AlmostEqual(double a, double b) => Math.Abs(a - b) < 0.5;

        /// <summary> Whether a time difference is nothing, or only the rounding of a shift. </summary>
        private static bool WithinShift(double difference, double shift) =>
            shift == 0 ? difference.AlmostEqual(0) : Math.Abs(difference) <= ShiftToleranceMs;

        private static double Vector2Distance(HitObject a, HitObject b)
        {
            var dx = a.Position.X - b.Position.X;
            var dy = a.Position.Y - b.Position.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string Format(double value) =>
            Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
    }
}
