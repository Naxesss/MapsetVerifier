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

        public static DiffResult Diff(Beatmap before, Beatmap after)
        {
            var changes = new List<Change>();
            var rollups = new List<Rollup>();

            var objectPairs = MatchObjects(
                before,
                after,
                out var oldOnly,
                out var newOnly,
                out var shift
            );

            foreach (var (oldObject, newObject, delta) in objectPairs)
                changes.AddRange(DiffObject(before, after, oldObject, newObject, delta));

            foreach (var o in oldOnly)
                changes.Add(ObjectChange(after, o, ChangeOp.Removed));

            foreach (var o in newOnly)
                changes.Add(ObjectChange(after, o, ChangeOp.Added));

            var timingAbsorbed = DiffTiming(before, after, shift, changes);

            if (shift != null)
            {
                var absorbed = objectPairs.Count(p => p.Delta != 0) + timingAbsorbed;
                rollups.Add(new Rollup(RollupKind.Offset, shift.Value, absorbed));
            }

            return new DiffResult(changes, rollups);
        }

        // ---------- Objects ----------

        private static List<(HitObject Old, HitObject New, double Delta)> MatchObjects(
            Beatmap before,
            Beatmap after,
            out List<HitObject> oldOnly,
            out List<HitObject> newOnly,
            out double? shift
        )
        {
            var pairs = new List<(HitObject, HitObject, double)>();
            var oldLeft = before.HitObjects.OrderBy(o => o.time).ToList();
            var newLeft = after.HitObjects.OrderBy(o => o.time).ToList();

            // Pass 1: same time and type. Chords and mania columns pair by the closest position.
            PairWhere(oldLeft, newLeft, pairs, 0, (o, n) => AlmostEqual(o.time, n.time));

            // Pass 2: a shift shared by most of what is left means everything moved in time.
            shift = DetectShift(oldLeft, newLeft);

            if (shift != null)
            {
                var delta = shift.Value;
                PairWhere(
                    oldLeft,
                    newLeft,
                    pairs,
                    delta,
                    (o, n) => AlmostEqual(o.time + delta, n.time)
                );
            }

            // Pass 3: small retimes in place.
            PairWhere(
                oldLeft,
                newLeft,
                pairs,
                null,
                (o, n) =>
                    Math.Abs(o.time - n.time) <= RetimeWindowMs
                    && Vector2Distance(o, n) <= RetimeMaxDistancePx
            );

            oldOnly = oldLeft;
            newOnly = newLeft;

            return pairs;
        }

        /// <summary>
        /// Greedily pairs objects of the same type that satisfy <paramref name="match" />, closest
        /// in position first. A null delta means "use the actual time difference".
        /// </summary>
        private static void PairWhere(
            List<HitObject> oldLeft,
            List<HitObject> newLeft,
            List<(HitObject, HitObject, double)> pairs,
            double? delta,
            Func<HitObject, HitObject, bool> match
        )
        {
            var candidates = new List<(HitObject Old, HitObject New, double Cost)>();
            var window = RetimeWindowMs + Math.Abs(delta ?? 0) + 1;
            var newTimes = newLeft.Select(n => n.time).ToArray();

            foreach (var o in oldLeft)
            {
                var type = o.GetObjectType();

                // Both lists are sorted by time, so only a window around o can match.
                for (
                    var i = LowerBound(newTimes, o.time + (delta ?? 0) - window);
                    i < newLeft.Count;
                    i++
                )
                {
                    var n = newLeft[i];

                    if (n.time > o.time + (delta ?? 0) + window)
                        break;

                    if (n.GetObjectType() != type || !match(o, n))
                        continue;

                    candidates.Add((o, n, Vector2Distance(o, n) + Math.Abs(o.time - n.time) / 10));
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
                pairs.Add((o, n, delta ?? n.time - o.time));
            }

            oldLeft.RemoveAll(usedOld.Contains);
            newLeft.RemoveAll(usedNew.Contains);
        }

        /// <summary> The time shift most unpaired objects agree on, if it covers enough of them. </summary>
        private static double? DetectShift(List<HitObject> oldLeft, List<HitObject> newLeft)
        {
            if (oldLeft.Count == 0 || newLeft.Count == 0)
                return null;

            var votes = new Dictionary<double, int>();
            var newTimes = newLeft.Select(n => n.time).ToArray();

            foreach (var o in oldLeft)
            {
                var type = o.GetObjectType();
                HitObject? closest = null;

                for (var i = LowerBound(newTimes, o.time - MaxShiftMs); i < newLeft.Count; i++)
                {
                    var n = newLeft[i];

                    if (n.time > o.time + MaxShiftMs)
                        break;

                    if (n.GetObjectType() != type)
                        continue;

                    // A shifted object stays roughly where it was; a remapped one does not vote.
                    if (Vector2Distance(o, n) > RetimeMaxDistancePx)
                        continue;

                    if (
                        closest == null
                        || Math.Abs(n.time - o.time) < Math.Abs(closest.time - o.time)
                    )
                        closest = n;
                }

                if (closest == null)
                    continue;

                var delta = Math.Round(closest.time - o.time);

                if (delta == 0 || Math.Abs(delta) > MaxShiftMs)
                    continue;

                votes[delta] = votes.GetValueOrDefault(delta) + 1;
            }

            if (votes.Count == 0)
                return null;

            var best = votes.OrderByDescending(v => v.Value).First();
            var needed = Math.Max(
                OffsetMinimumObjects,
                (int)Math.Ceiling(oldLeft.Count * OffsetShare)
            );

            return best.Value >= needed ? best.Key : null;
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

            // A shift shared with the rollup is not a change of its own; anything on top of it is.
            var retimed = n.time - o.time - delta;

            if (!retimed.AlmostEqual(0))
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
            else if (distance > 0.001)
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

            if (wasNewCombo != isNewCombo)
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

            if (type == "Slider")
                foreach (var change in DiffSlider(before, after, o, n, reference))
                    yield return change;
            else if (type is "Spinner" or "Hold note")
            {
                var oldEnd = o.GetEndTime() - delta;
                var newEnd = n.GetEndTime();

                if (!oldEnd.AlmostEqual(newEnd))
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

            if (shapeChanged)
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

        /// <summary> Diffs timing lines; returns how many were absorbed by the shared time shift. </summary>
        private static int DiffTiming(
            Beatmap before,
            Beatmap after,
            double? shift,
            List<Change> changes
        )
        {
            var oldLeft = before.TimingLines.ToList();
            var newLeft = after.TimingLines.ToList();
            var absorbed = 0;

            void Pair(double delta)
            {
                foreach (var o in oldLeft.ToList())
                {
                    var n = newLeft.FirstOrDefault(l =>
                        l.Uninherited == o.Uninherited && AlmostEqual(o.Offset + delta, l.Offset)
                    );

                    if (n == null)
                        continue;

                    oldLeft.Remove(o);
                    newLeft.Remove(n);

                    if (delta != 0)
                        absorbed++;

                    changes.AddRange(DiffLine(o, n));
                }
            }

            Pair(0);

            if (shift != null)
                Pair(shift.Value);

            foreach (var l in oldLeft)
                changes.Add(LineChange(l, ChangeOp.Removed));

            foreach (var l in newLeft)
                changes.Add(LineChange(l, ChangeOp.Added));

            return absorbed;
        }

        private static Change LineChange(TimingLine line, ChangeOp op) =>
            new(
                ChangeKind.Timing,
                op,
                line.Offset,
                Field: line.Uninherited ? "RedLine" : "GreenLine"
            );

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
