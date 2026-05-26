using System.Text;
using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using MathNet.Numerics;
using Serilog;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class HitObjectsTranslator : DiffTranslator
    {
        public override string Section => "HitObjects";
        public override string TranslatedSection => "Hit Objects";

        public class ShiftSection
        {
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public double Shift { get; set; }
            public List<UnifiedStep> Steps { get; } = new List<UnifiedStep>();
            public bool IsSectionShift { get; set; }
        }

        public class UnifiedStep
        {
            public HitObject? OldObj { get; set; }
            public HitObject? NewObj { get; set; }
            public double? Shift => (OldObj != null && NewObj != null) ? (double?)(NewObj.time - OldObj.time) : null;
        }

        private static double GetStepTime(UnifiedStep step)
        {
            if (step.NewObj != null) return step.NewObj.time;
            if (step.OldObj != null) return step.OldObj.time;
            return 0;
        }

        private static HitObject ParseHitObject(string line, Beatmap beatmap)
        {
            var args = line.Split(',');
            return beatmap.GeneralSettings.mode switch
            {
                Beatmap.Mode.Catch => HitObject.HasType(args, HitObject.Types.Circle)
                    ? new Fruit(args, beatmap)
                : HitObject.HasType(args, HitObject.Types.Slider) && args.Length >= 8
                    ? new JuiceStream(args, beatmap)
                : new Bananas(args, beatmap),
                _ => HitObject.HasType(args, HitObject.Types.Circle)
                    ? new Circle(args, beatmap)
                : HitObject.HasType(args, HitObject.Types.Slider) && args.Length >= 8
                    ? new Slider(args, beatmap)
                : HitObject.HasType(args, HitObject.Types.ManiaHoldNote) && args.Length >= 6 && args[5].Contains(":")
                    ? new HoldNote(args, beatmap)
                : HitObject.HasType(args, HitObject.Types.Spinner) && args.Length >= 6 && !args[5].Contains(":")
                    ? new Spinner(args, beatmap)
                : new HitObject(args, beatmap),
            };
        }

        private static List<HitObject> ParseHitObjectsFromCode(string code, Beatmap beatmap)
        {
            var hitObjects = new List<HitObject>();
            var lines = code.Replace("\r", "").Split('\n');
            bool inHitObjectsSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inHitObjectsSection = (trimmed == "[HitObjects]");
                    continue;
                }

                if (inHitObjectsSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    try
                    {
                        var hitObject = ParseHitObject(trimmed, beatmap);
                        hitObjects.Add(hitObject);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not parse old hit object from snapshot code");
                    }
                }
            }

            for (var i = 0; i < hitObjects.Count; ++i)
                hitObjects[i].SetHitObjectIndex(i);

            return hitObjects;
        }

        // Drift from the estimated global shift carries a stronger penalty than raw time
        // distance so DTW prefers correctly-shifted matches over cheap-but-wrong local
        // matches (same problem as in TimingTranslator).
        private const double ShiftDriftWeight = 0.5;

        // Distance function used by DTW to pair objects across snapshots.
        // Mode-aware because each ruleset cares about different attributes:
        //   - Standard: full XY position.
        //   - Catch:    only X matters (vertical position is ignored gameplay-wise).
        //   - Taiko:    position is irrelevant; hitsounds encode the gameplay object (don/kat/finish).
        //   - Mania:    X = column (lane); same lane is a hard match, different lane heavily penalised.
        private static double GetDistance(HitObject x, HitObject y, Beatmap.Mode mode, double globalShift)
        {
            double cost = 0;

            if (x.GetObjectType() != y.GetObjectType())
            {
                // Type changes (e.g. Circle <-> Slider) are surfaced as paired remove +
                // add entries instead of a cross-type DTW match — much clearer than a
                // "Slider changed" entry with a giant position move buried in details.
                // A large finite value (rather than PositiveInfinity) keeps the DP math
                // well-behaved during back-tracking comparisons.
                return 1e9;
            }

            // 1. Spatial term, mode-aware.
            switch (mode)
            {
                case Beatmap.Mode.Taiko:
                    // No position. Hitsounds carry the gameplay identity.
                    if (x.hitSound != y.hitSound) cost += 200.0;
                    break;

                case Beatmap.Mode.Catch:
                    cost += Math.Log(1.0 + Math.Abs(x.Position.X - y.Position.X)) * 15.0;
                    break;

                case Beatmap.Mode.Mania:
                    // Different lane is essentially a different object.
                    if (Math.Abs(x.Position.X - y.Position.X) > 0.5) cost += 200.0;
                    break;

                default:
                    cost += Math.Log(1.0 + Vector2.Distance(x.Position, y.Position)) * 15.0;
                    break;
            }

            // 2. Extras mismatch (sampleset/addition/custom index/volume/filename).
            if (x.sampleset != y.sampleset) cost += 10.0;
            if (x.addition != y.addition) cost += 10.0;
            if (x.customIndex != y.customIndex) cost += 10.0;
            if (x.volume != y.volume) cost += 10.0;
            if (x.filename != y.filename) cost += 20.0;

            // 3. Object-specific properties (excluding hitsounds for non-taiko modes).
            if (x is Slider xSlider && y is Slider ySlider)
            {
                if (xSlider.CurveType != ySlider.CurveType) cost += 50.0;
                if (xSlider.EdgeAmount != ySlider.EdgeAmount) cost += 50.0;
                if (xSlider.NodePositions.Count != ySlider.NodePositions.Count) cost += 50.0;
            }
            else if (x is Spinner xSpinner && y is Spinner ySpinner)
            {
                double xLen = xSpinner.endTime - xSpinner.time;
                double yLen = ySpinner.endTime - ySpinner.time;
                cost += Math.Abs(xLen - yLen) * 0.1;
            }
            else if (x is HoldNote xNote && y is HoldNote yNote)
            {
                double xLen = xNote.endTime - xNote.time;
                double yLen = yNote.endTime - yNote.time;
                cost += Math.Abs(xLen - yLen) * 0.1;
            }

            // 4. Penalise deviation from the dominant shift rather than raw time distance.
            // Otherwise DTW pairs an old object to a nearby unrelated new one because the
            // absolute gap is small, even when the section was shifted by, say, +2009ms.
            double drift = (y.time - x.time) - globalShift;
            cost += Math.Abs(drift) * ShiftDriftWeight;

            // 5. Flat penalty when anything but a perfect match (mode-aware for position).
            bool positionsMatch = mode switch
            {
                Beatmap.Mode.Taiko => true, // position irrelevant
                Beatmap.Mode.Catch => x.Position.X == y.Position.X,
                Beatmap.Mode.Mania => Math.Abs(x.Position.X - y.Position.X) < 0.5,
                _ => x.Position == y.Position,
            };
            bool isExactMatch = x.GetObjectType() == y.GetObjectType() &&
                                positionsMatch &&
                                x.sampleset == y.sampleset &&
                                x.addition == y.addition &&
                                (x.customIndex ?? 0) == (y.customIndex ?? 0) &&
                                (x.volume ?? 0) == (y.volume ?? 0) &&
                                x.filename == y.filename;

            if (mode == Beatmap.Mode.Taiko)
                isExactMatch = isExactMatch && x.hitSound == y.hitSound;

            if (x is Slider xs && y is Slider ys)
            {
                isExactMatch = isExactMatch &&
                               xs.CurveType == ys.CurveType &&
                               xs.EdgeAmount == ys.EdgeAmount &&
                               xs.NodePositions.Count == ys.NodePositions.Count;
            }

            if (!isExactMatch)
                cost += 20.0;

            return cost;
        }

        private static List<Tuple<int, int>> AlignHitObjects(
            List<HitObject> oldList,
            List<HitObject> newList,
            Beatmap.Mode mode,
            double globalShift,
            double maxWindow = 5000.0
        )
        {
            int n = oldList.Count;
            int m = newList.Count;

            double[,] dp = new double[n + 1, m + 1];
            double gapPenalty = 300.0;

            for (int i = 0; i <= n; i++)
                dp[i, 0] = i * gapPenalty;
            for (int j = 0; j <= m; j++)
                dp[0, j] = j * gapPenalty;

            for (int i = 1; i <= n; i++)
            {
                var oldObj = oldList[i - 1];
                for (int j = 1; j <= m; j++)
                {
                    var newObj = newList[j - 1];
                    double timeDiff = Math.Abs(oldObj.time - newObj.time);

                    if (timeDiff <= maxWindow)
                    {
                        double matchCost = GetDistance(oldObj, newObj, mode, globalShift);
                        double optMatch = dp[i - 1, j - 1] + matchCost;
                        double optDelete = dp[i - 1, j] + gapPenalty;
                        double optInsert = dp[i, j - 1] + gapPenalty;

                        dp[i, j] = Math.Min(optMatch, Math.Min(optDelete, optInsert));
                    }
                    else
                    {
                        double optDelete = dp[i - 1, j] + gapPenalty;
                        double optInsert = dp[i, j - 1] + gapPenalty;
                        dp[i, j] = Math.Min(optDelete, optInsert);
                    }
                }
            }

            var path = new List<Tuple<int, int>>();
            int currI = n;
            int currJ = m;

            while (currI > 0 || currJ > 0)
            {
                if (currI > 0 && currJ > 0)
                {
                    var oldObj = oldList[currI - 1];
                    var newObj = newList[currJ - 1];
                    double timeDiff = Math.Abs(oldObj.time - newObj.time);

                    if (timeDiff <= maxWindow)
                    {
                        double matchCost = GetDistance(oldObj, newObj, mode, globalShift);
                        if (Math.Abs(dp[currI, currJ] - (dp[currI - 1, currJ - 1] + matchCost)) < 1e-5)
                        {
                            path.Add(new Tuple<int, int>(currI - 1, currJ - 1));
                            currI--;
                            currJ--;
                            continue;
                        }
                    }
                }

                if (currI > 0 && Math.Abs(dp[currI, currJ] - (dp[currI - 1, currJ] + gapPenalty)) < 1e-5)
                {
                    path.Add(new Tuple<int, int>(currI - 1, -1));
                    currI--;
                }
                else if (currJ > 0)
                {
                    path.Add(new Tuple<int, int>(-1, currJ - 1));
                    currJ--;
                }
                else
                {
                    break;
                }
            }

            path.Reverse();
            return path;
        }

        public override IEnumerable<DiffInstance> Translate(
            IEnumerable<DiffInstance> diffs,
            Beatmap beatmap
        )
        {
            if (beatmap == null)
                yield break;

            var diffsList = diffs.ToList();
            if (!diffsList.Any())
                yield break;

            var snapshotCreationDate = diffsList.First().SnapshotCreationDate;
            string? oldCode = null;
            string? newCode = null;
            var beatmapSetId = beatmap.MetadataSettings.beatmapSetId?.ToString();
            var beatmapId = beatmap.MetadataSettings.beatmapId?.ToString();

            if (beatmapSetId != null && beatmapId != null)
            {
                try
                {
                    var snapshots = Snapshotter.GetSnapshots(beatmapSetId, beatmapId).OrderBy(s => s.creationTime).ToList();
                    var snapshotIndex = snapshots.FindIndex(s => s.creationTime == snapshotCreationDate);
                    if (snapshotIndex != -1)
                    {
                        oldCode = snapshots[snapshotIndex].code;
                        if (snapshotIndex + 1 < snapshots.Count)
                        {
                            newCode = snapshots[snapshotIndex + 1].code;
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not load snapshot for DTW alignment");
                }
            }

            if (string.IsNullOrEmpty(oldCode))
            {
                var addedHitObjects = new List<Tuple<DiffInstance, HitObject>>();
                var removedHitObjects = new List<Tuple<DiffInstance, HitObject>>();

                foreach (var diff in diffsList)
                {
                    HitObject? hitObject = null;

                    try
                    {
                        hitObject = new HitObject(diff.Diff.Split(','), beatmap);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not translate hit object");
                    }

                    if (hitObject != null)
                    {
                        if (diff.DiffType == DiffType.Added)
                            addedHitObjects.Add(new Tuple<DiffInstance, HitObject>(diff, hitObject));
                        else
                            removedHitObjects.Add(new Tuple<DiffInstance, HitObject>(diff, hitObject));
                    }
                    else
                    // Failing to parse a changed line shouldn't stop it from showing.
                    {
                        yield return diff;
                    }
                }

                foreach (var (addedDiff, addedObject) in addedHitObjects)
                {
                    var stamp = Timestamp.Get(addedObject.time);
                    var type = addedObject.GetObjectType();

                    var found = false;
                    var removedObjects = removedHitObjects.Select(tuple => tuple.Item2).ToList();

                    foreach (var removedObject in removedObjects)
                        if (addedObject.time.AlmostEqual(removedObject.time))
                        {
                            var removedType = removedObject.GetObjectType();

                            if (type != removedType)
                                continue;

                            var changes = GetChanges(addedObject, removedObject, beatmap).ToList();

                            if (changes.Count == 1)
                                yield return new DiffInstance(
                                    stamp + changes[0],
                                    Section,
                                    DiffType.Changed,
                                    new List<string>(),
                                    addedDiff.SnapshotCreationDate
                                );
                            else if (changes.Count > 1)
                                yield return new DiffInstance(
                                    stamp + type + " changed.",
                                    Section,
                                    DiffType.Changed,
                                    changes,
                                    addedDiff.SnapshotCreationDate
                                );

                            found = true;
                            var o = removedObject;
                            removedHitObjects.RemoveAll(tuple => tuple.Item2.code == o.code);
                        }

                    if (!found)
                        yield return new DiffInstance(
                            stamp + type + " added.",
                            Section,
                            DiffType.Added,
                            new List<string>(),
                            addedDiff.SnapshotCreationDate
                        );
                }

                foreach (var removedTuple in removedHitObjects)
                {
                    var removedDiff = removedTuple.Item1;
                    var removedObject = removedTuple.Item2;

                    // No DTW available on the fallback path so shift is unknown; pass 0
                    // through BuildRemovedStamp to keep the same "(originally ...)" format
                    // as the main path for visual consistency.
                    var stamp = BuildRemovedStamp(removedObject.time, 0);
                    var type = removedObject.GetObjectType();

                    yield return new DiffInstance(
                        stamp + type + " removed.",
                        Section,
                        DiffType.Removed,
                        new List<string>(),
                        removedDiff.SnapshotCreationDate
                    );
                }

                yield break;
            }

            // Parse old hit objects
            var oldHitObjects = ParseHitObjectsFromCode(oldCode, beatmap);
            var newHitObjects = !string.IsNullOrEmpty(newCode)
                ? ParseHitObjectsFromCode(newCode, beatmap)
                : beatmap.HitObjects;

            // Get shift sections from helper. The returned globalShift is the dominant
            // pairwise offset used by DTW; we re-use it below to display orphan removals
            // at their shifted time.
            var (sections, globalShiftHint) = GetShiftSections(beatmap, oldCode, newCode);

            // Yield diffs for each section
            foreach (var section in sections)
            {
                int matchedCount = section.Steps.Count(s => s.Shift != null && Math.Abs(s.Shift.Value - section.Shift) <= ShiftTolerance);

                if (section.IsSectionShift)
                {
                    var stamp = Timestamp.Get(section.StartTime);
                    var sign = section.Shift > 0 ? "+" : "";

                    yield return new DiffInstance(
                        stamp + $"Section shifted in time by {sign}{section.Shift:0.##} ms ({matchedCount} objects).",
                        Section,
                        DiffType.Changed,
                        new List<string>(),
                        snapshotCreationDate
                    );
                }

                // Emit residuals flat (separate entries, never nested inside the shift summary):
                // - Net additions / removals (no counterpart in shifted space)
                // - Matched objects whose shift drifts beyond +/-2ms of the section shift
                // - Matched objects with non-time property changes (position, hitsounds, ...)
                // Matched objects that are purely on-shift with no other change are suppressed.
                foreach (var s in section.Steps)
                {
                    var typeObj = s.NewObj?.GetObjectType() ?? s.OldObj?.GetObjectType() ?? "Object";

                    if (s.OldObj != null && s.NewObj == null)
                    {
                        // Render at the shifted time so removals appear in the same place
                        // as surviving / replacement objects in the new timeline; keep the
                        // pre-shift time in parentheses so the user can still find it in
                        // the old beatmap.
                        var stampObj = BuildRemovedStamp(s.OldObj.time, globalShiftHint);
                        yield return new DiffInstance(
                            stampObj + typeObj + " removed.",
                            Section,
                            DiffType.Removed,
                            new List<string>(),
                            snapshotCreationDate
                        );
                    }
                    else if (s.OldObj == null && s.NewObj != null)
                    {
                        var stampObj = Timestamp.Get(s.NewObj.time);
                        yield return new DiffInstance(
                            stampObj + typeObj + " added.",
                            Section,
                            DiffType.Added,
                            new List<string>(),
                            snapshotCreationDate
                        );
                    }
                    else if (s.OldObj != null && s.NewObj != null)
                    {
                        var stampObj = Timestamp.Get(s.NewObj.time);
                        var changes = GetChanges(s.NewObj, s.OldObj, beatmap).ToList();
                        double localShift = s.NewObj.time - s.OldObj.time;
                        double driftFromSection = localShift - section.Shift;
                        double snapTolerance = GetSnapTolerance(beatmap, s.NewObj.time);
                        bool aligned = Math.Abs(driftFromSection) <= snapTolerance;
                        if (section.IsSectionShift)
                        {
                            if (!aligned)
                            {
                                var localSign = driftFromSection > 0 ? "+" : "";
                                // If the object's new time is still on a valid snap, treat
                                // the drift as a re-snap (timing adjustment) and route it to
                                // the Timing tab as its own entry. Property changes stay
                                // on Hit Objects.
                                if (IsOnSnap(beatmap, s.NewObj.time))
                                {
                                    // Display under the Timing tab. The Snapshotter only
                                    // auto-rewrites Section when it still matches the input
                                    // section, so using "Timing" here keeps it routed.
                                    yield return new DiffInstance(
                                        stampObj + $"Object re-snapped by {localSign}{driftFromSection:0.##} ms (total shift {localShift:0.##} ms).",
                                        "Timing",
                                        DiffType.Changed,
                                        new List<string>(),
                                        snapshotCreationDate
                                    );
                                }
                                else
                                {
                                    changes.Insert(0, $"Time drifts from section shift by {localSign}{driftFromSection:0.##} ms (object shift {localShift:0.##} ms).");
                                }
                            }
                            // else: aligned -> covered by the section shift entry, skip time note.
                        }
                        else if (Math.Abs(localShift) >= 1.0)
                        {
                            // Singleton time shift (didn't cluster into any section). If
                            // the new time is snap-aligned, treat it as a timing change
                            // (e.g. one object nudged by a beat division) and route it to
                            // the Timing tab. Only fall back to a hit-object Time-changed
                            // note when the new time is genuinely off-snap.
                            var localSign = localShift > 0 ? "+" : "";
                            if (IsOnSnap(beatmap, s.NewObj.time))
                            {
                                yield return new DiffInstance(
                                    stampObj + $"Object re-snapped by {localSign}{localShift:0.##} ms.",
                                    "Timing",
                                    DiffType.Changed,
                                    new List<string>(),
                                    snapshotCreationDate
                                );
                            }
                            else
                            {
                                changes.Insert(0, $"Time changed from {s.OldObj.time} ms to {s.NewObj.time} ms ({localSign}{localShift:0.##} ms).");
                            }
                        }

                        // Always emit matched-pair changes under a "{type} changed." title
                        // with details, so single-change entries (e.g. "Pixel length changed
                        // from 160 to 175") stay visibly attached to their object type rather
                        // than floating as ambiguous standalone lines.
                        if (changes.Count > 0)
                        {
                            yield return new DiffInstance(
                                stampObj + typeObj + " changed.",
                                Section,
                                DiffType.Changed,
                                changes,
                                snapshotCreationDate
                            );
                        }
                    }
                }
            }
        }

        // +/-2ms drift is tolerated as "on the same snap" relative to a section shift.
        // Used for RANSAC inlier counting (tight, so the average shift is precise).
        private const double ShiftTolerance = 2.0;

        // Per-step drift report uses 1/32 of the active beat as tolerance, absorbing
        // sub-snap jitter into the section shift. Falls back to ShiftTolerance if no
        // active uninherited line.
        private static double GetSnapTolerance(Beatmap beatmap, double time)
        {
            var uninherited = beatmap.GetTimingLine<UninheritedLine>(time);
            if (uninherited == null) return ShiftTolerance;
            return uninherited.msPerBeat / 32.0;
        }

        // Build a "<shifted> (originally <old>) - " prefix for orphan-removed entries
        // when a non-zero global shift was detected (so the user can cross-reference the
        // pre-shift time in the old beatmap). Falls back to a plain timestamp when no
        // shift exists — the parenthetical would just repeat the primary stamp.
        private static string BuildRemovedStamp(double oldTime, double shift)
        {
            if (Math.Abs(shift) < 1.0)
                return Timestamp.Get(oldTime);
            string oldTrim = Timestamp.Get(oldTime).TrimEnd(' ', '-');
            string shiftedTrim = Timestamp.Get(oldTime + shift).TrimEnd(' ', '-');
            return $"{shiftedTrim} (originally {oldTrim}) - ";
        }

        // "On-snap" means the object's time aligns with a common beat divisor within ~2ms.
        // Used to decide whether a drift looks like an intentional re-snap (route to
        // Timing tab) or like an unsnap (keep on the Hit Objects tab).
        private static bool IsOnSnap(Beatmap beatmap, double time)
        {
            var line = beatmap.GetTimingLine<UninheritedLine>(time);
            if (line == null) return false;
            try
            {
                double unsnap = beatmap.GetPracticalUnsnap(time, line);
                return Math.Abs(unsnap) <= 2.0;
            }
            catch
            {
                // GetPracticalUnsnap can throw on corrupt timing data (NaN).
                return false;
            }
        }

        // Minimum inliers for the global shift to be reported as a section shift on the
        // Hit Objects tab. Higher than timing because hit objects are usually plentiful and
        // a few coincidentally-aligned objects shouldn't trigger a shift report.
        private const int MinShiftInliersHitObjects = 5;

        public static (List<ShiftSection> sections, double globalShift) GetShiftSections(
            Beatmap beatmap,
            string oldCode,
            string? newCode
        )
        {
            var mode = beatmap.GeneralSettings.mode;

            var oldHitObjects = ParseHitObjectsFromCode(oldCode, beatmap);
            var newHitObjects = !string.IsNullOrEmpty(newCode)
                ? ParseHitObjectsFromCode(newCode, beatmap)
                : beatmap.HitObjects;

            // Estimate the dominant shift via a histogram across all candidate pairs,
            // so DTW prefers matches whose shift agrees with the global consensus over
            // cheap-but-wrong local matches.
            double globalShiftHint = ShiftRansac.EstimateGlobalShift(
                oldHitObjects, newHitObjects,
                h => h.time, h => h.time,
                (o, n) => o.GetObjectType() == n.GetObjectType(),
                5000.0
            );

            // Align old and new hit objects using DTW (Needleman-Wunsch with a time window).
            var alignment = AlignHitObjects(oldHitObjects, newHitObjects, mode, globalShiftHint, 5000.0);

            // Convert alignment into UnifiedSteps.
            var steps = new List<UnifiedStep>();
            foreach (var step in alignment)
            {
                var unified = new UnifiedStep();
                if (step.Item1 != -1)
                    unified.OldObj = oldHitObjects[step.Item1];
                if (step.Item2 != -1)
                    unified.NewObj = newHitObjects[step.Item2];
                steps.Add(unified);
            }

            // Cluster shifts greedily within +/-ShiftTolerance. Any cluster with at least
            // MinShiftInliersHitObjects members becomes its own section, so a group of
            // objects shifted uniformly is collapsed into a single shift entry rather than
            // emitting one Changed entry per object. Mania chord notes are deduplicated by
            // old.time so a single chord doesn't multi-count toward the inlier threshold.
            var seenManiaChordTimes = mode == Beatmap.Mode.Mania ? new HashSet<double>() : null;
            var clusters = new List<List<UnifiedStep>>();
            var unmatched = new List<UnifiedStep>();

            foreach (var step in steps)
            {
                if (!step.Shift.HasValue)
                {
                    unmatched.Add(step);
                    continue;
                }

                long shift = (long)Math.Round(step.Shift.Value);
                var existing = clusters.FirstOrDefault(c =>
                    Math.Abs((long)Math.Round(c[0].Shift!.Value) - shift) <= ShiftTolerance);
                if (existing != null) existing.Add(step);
                else clusters.Add(new List<UnifiedStep> { step });
            }

            var sections = new List<ShiftSection>();

            foreach (var cluster in clusters)
            {
                long shift = (long)Math.Round(cluster[0].Shift!.Value);

                int votingCount;
                if (seenManiaChordTimes != null)
                {
                    seenManiaChordTimes.Clear();
                    votingCount = 0;
                    foreach (var s in cluster)
                    {
                        if (s.OldObj != null && seenManiaChordTimes.Add(s.OldObj.time))
                            votingCount++;
                    }
                }
                else
                {
                    votingCount = cluster.Count;
                }

                bool isShift = Math.Abs(shift) >= 1 && votingCount >= MinShiftInliersHitObjects;

                if (isShift)
                {
                    var sec = new ShiftSection
                    {
                        Shift = shift,
                        StartTime = cluster.Min(GetStepTime),
                        EndTime = cluster.Max(GetStepTime),
                        IsSectionShift = true,
                    };
                    sec.Steps.AddRange(cluster);
                    sections.Add(sec);
                }
                else
                {
                    foreach (var step in cluster)
                    {
                        var sec = new ShiftSection
                        {
                            Shift = step.Shift ?? 0.0,
                            StartTime = GetStepTime(step),
                            EndTime = GetStepTime(step),
                            IsSectionShift = false,
                        };
                        sec.Steps.Add(step);
                        sections.Add(sec);
                    }
                }
            }

            foreach (var step in unmatched)
            {
                var sec = new ShiftSection
                {
                    Shift = 0.0,
                    StartTime = GetStepTime(step),
                    EndTime = GetStepTime(step),
                    IsSectionShift = false,
                };
                sec.Steps.Add(step);
                sections.Add(sec);
            }

            return (sections.OrderBy(s => s.StartTime).ToList(), globalShiftHint);
        }

        /// <summary>
        /// Handle taiko with custom logic given their hitobject types are based on hitsounds
        /// </summary>
        private static string? GetTaikoHitSoundChange(
            HitObject addedObject,
            HitObject removedObject
        )
        {
            var current = addedObject.GetHitSounds().ToList();
            var old = removedObject.GetHitSounds().ToList();

            var added = current.Except(old);
            var removed = old.Except(current);

            // No changes so nothing to return
            if (!added.Any() && !removed.Any())
            {
                return null;
            }

            var isBig = current.Contains(HitObject.HitSounds.Finish);

            var isKat =
                current.Contains(HitObject.HitSounds.Clap)
                || current.Contains(HitObject.HitSounds.Whistle);

            var builder = new StringBuilder("Changed to ");
            if (addedObject.HasType(HitObject.Types.Slider))
            {
                builder.Append(isBig ? "Big " : "Regular ");
                builder.Append("Slider.");
            }
            else
            {
                builder.Append(isBig ? "Big " : "");
                builder.Append(isKat ? "Kat." : "Don.");
            }

            return builder.ToString();
        }

        private static string GetHitSoundChange(
            bool isAddition,
            HitObject.HitSounds hitSound,
            HitObject addedObject,
            HitObject removedObject,
            Beatmap beatmap
        )
        {
            var prefix = isAddition ? "Addition " : "Removal ";
            var suffix = "";

            if (addedObject.type.HasFlag(HitObject.Types.Slider))
            {
                suffix = " to head";
            }

            var hitSoundName = Enum.GetName(hitSound)?.ToLower();

            return prefix + hitSoundName + suffix + ".";
        }

        private static IEnumerable<string> GetChanges(
            HitObject addedObject,
            HitObject removedObject,
            Beatmap beatmap
        )
        {
            if (addedObject.Position != removedObject.Position)
                yield return "Moved from ("
                    + removedObject.Position.X
                    + "; "
                    + removedObject.Position.Y
                    + ") to ("
                    + addedObject.Position.X
                    + "; "
                    + addedObject.Position.Y
                    + ").";

            if (addedObject.hitSound != removedObject.hitSound)
            {
                if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
                {
                    var taikoHitSoundChange = GetTaikoHitSoundChange(addedObject, removedObject);
                    if (taikoHitSoundChange != null)
                    {
                        yield return taikoHitSoundChange;
                    }
                }
                else
                {
                    foreach (
                        HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds))
                    )
                    {
                        if (
                            addedObject.HasHitSound(hitSound)
                            && !removedObject.HasHitSound(hitSound)
                        )
                        {
                            yield return GetHitSoundChange(
                                true,
                                hitSound,
                                addedObject,
                                removedObject,
                                beatmap
                            );
                        }

                        if (
                            !addedObject.HasHitSound(hitSound)
                            && removedObject.HasHitSound(hitSound)
                        )
                        {
                            yield return GetHitSoundChange(
                                false,
                                hitSound,
                                addedObject,
                                removedObject,
                                beatmap
                            );
                        }
                    }
                }
            }

            if (addedObject.sampleset != removedObject.sampleset)
                yield return "Sampleset changed from "
                    + removedObject.sampleset.ToString().ToLower()
                    + " to "
                    + addedObject.sampleset.ToString().ToLower()
                    + ".";

            if (addedObject.addition != removedObject.addition)
                yield return "Addition changed from "
                    + removedObject.addition.ToString().ToLower()
                    + " to "
                    + addedObject.addition.ToString().ToLower()
                    + ".";

            if ((addedObject.customIndex ?? 0) != (removedObject.customIndex ?? 0))
                yield return "Custom sampleset index override changed from "
                    + (removedObject.customIndex?.ToString() ?? "default")
                    + " to "
                    + (addedObject.customIndex?.ToString() ?? "default")
                    + ".";

            if (
                addedObject.type.HasFlag(HitObject.Types.NewCombo)
                && !removedObject.type.HasFlag(HitObject.Types.NewCombo)
            )
                yield return "Added new combo.";

            if (
                !addedObject.type.HasFlag(HitObject.Types.NewCombo)
                && removedObject.type.HasFlag(HitObject.Types.NewCombo)
            )
                yield return "Removed new combo.";

            var addedComboSkip = 0;

            if (addedObject.type.HasFlag(HitObject.Types.ComboSkip1))
                addedComboSkip += 1;

            if (addedObject.type.HasFlag(HitObject.Types.ComboSkip2))
                addedComboSkip += 2;

            if (addedObject.type.HasFlag(HitObject.Types.ComboSkip3))
                addedComboSkip += 4;

            var removedComboSkip = 0;

            if (removedObject.type.HasFlag(HitObject.Types.ComboSkip1))
                removedComboSkip += 1;

            if (removedObject.type.HasFlag(HitObject.Types.ComboSkip2))
                removedComboSkip += 2;

            if (removedObject.type.HasFlag(HitObject.Types.ComboSkip3))
                removedComboSkip += 4;

            if (addedComboSkip != removedComboSkip)
                yield return "Changed skipped combo amount from "
                    + removedComboSkip
                    + " to "
                    + addedComboSkip
                    + ".";

            if (addedObject.filename != removedObject.filename)
                yield return "Hit sound filename changed from "
                    + removedObject.filename
                    + " to "
                    + addedObject.filename
                    + ".";

            if (addedObject.volume != removedObject.volume)
                yield return "Hit sound volume changed from "
                    + (removedObject.volume?.ToString() ?? "inherited")
                    + " to "
                    + (addedObject.volume?.ToString() ?? "inherited")
                    + ".";

            if (addedObject is Slider addedSlider && removedObject is Slider removedSlider)
            {
                if (addedSlider.CurveType != removedSlider.CurveType)
                    yield return "Curve type changed from "
                        + removedSlider.CurveType
                        + " to "
                        + addedSlider.CurveType
                        + ".";

                if (addedSlider.EdgeAmount != removedSlider.EdgeAmount)
                    yield return "Reverse amount changed from "
                        + (removedSlider.EdgeAmount - 1)
                        + " to "
                        + (addedSlider.EdgeAmount - 1)
                        + ".";

                if (addedSlider.EndSampleset != removedSlider.EndSampleset)
                    yield return "Tail sampleset changed from "
                        + removedSlider.EndSampleset.ToString().ToLower()
                        + " to "
                        + addedSlider.EndSampleset.ToString().ToLower()
                        + ".";

                if (addedSlider.EndAddition != removedSlider.EndAddition)
                    yield return "Tail addition changed from "
                        + removedSlider.EndAddition.ToString().ToLower()
                        + " to "
                        + addedSlider.EndAddition.ToString().ToLower()
                        + ".";

                if (addedSlider.EndHitSound != removedSlider.EndHitSound)
                    foreach (
                        HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds))
                    )
                    {
                        if (
                            addedSlider.EndHitSound.HasFlag(hitSound)
                            && !removedSlider.EndHitSound.HasFlag(hitSound)
                        )
                            yield return "Added "
                                + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower()
                                + " to tail.";

                        if (
                            !addedSlider.EndHitSound.HasFlag(hitSound)
                            && removedSlider.EndHitSound.HasFlag(hitSound)
                        )
                            yield return "Removed "
                                + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower()
                                + " from tail.";
                    }

                if (!addedSlider.PixelLength.AlmostEqual(removedSlider.PixelLength))
                    yield return "Pixel length changed from "
                        + removedSlider.PixelLength
                        + " to "
                        + addedSlider.PixelLength
                        + ".";

                if (addedSlider.NodePositions.Count == removedSlider.NodePositions.Count)
                {
                    // The first node is the start, which we already checked.
                    for (var i = 1; i < addedSlider.NodePositions.Count; ++i)
                        if (addedSlider.NodePositions[i] != removedSlider.NodePositions[i])
                            yield return "Node "
                                + (i + 1)
                                + " moved from ("
                                + removedSlider.NodePositions[i].X
                                + "; "
                                + removedSlider.NodePositions[i].Y
                                + ") to ("
                                + addedSlider.NodePositions[i].X
                                + "; "
                                + addedSlider.NodePositions[i].Y
                                + ").";
                }
                else
                {
                    yield return "Node count changed from "
                        + removedSlider.NodePositions.Count
                        + " to "
                        + addedSlider.NodePositions.Count
                        + " (possibly positions as well).";
                }

                if (addedSlider.EdgeAmount == removedSlider.EdgeAmount)
                {
                    for (var i = 0; i < addedSlider.ReverseSamplesets.Count; ++i)
                        if (
                            addedSlider.ReverseSamplesets.ElementAtOrDefault(i)
                            != removedSlider.ReverseSamplesets.ElementAtOrDefault(i)
                        )
                            yield return "Reverse #"
                                + (i + 1)
                                + " sampleset changed from "
                                + removedSlider
                                    .ReverseSamplesets.ElementAtOrDefault(i)
                                    .ToString()
                                    .ToLower()
                                    + " to "
                                + addedSlider
                                    .ReverseSamplesets.ElementAtOrDefault(i)
                                    .ToString()
                                    .ToLower()
                                + ".";

                    for (var i = 0; i < addedSlider.ReverseAdditions.Count; ++i)
                        if (
                            addedSlider.ReverseAdditions.ElementAtOrDefault(i)
                            != removedSlider.ReverseAdditions.ElementAtOrDefault(i)
                        )
                            yield return "Reverse #"
                                + (i + 1)
                                + " addition changed from "
                                + removedSlider
                                    .ReverseAdditions.ElementAtOrDefault(i)
                                    .ToString()
                                    .ToLower()
                                    + " to "
                                + addedSlider
                                    .ReverseAdditions.ElementAtOrDefault(i)
                                    .ToString()
                                    .ToLower()
                                + ".";

                    for (var i = 0; i < addedSlider.ReverseAdditions.Count; ++i)
                        if (
                            addedSlider.ReverseHitSounds.ElementAtOrDefault(i)
                            != removedSlider.ReverseHitSounds.ElementAtOrDefault(i)
                        )
                            foreach (
                                HitObject.HitSounds hitSound in Enum.GetValues(
                                    typeof(HitObject.HitSounds)
                                )
                            )
                            {
                                if (
                                    addedSlider
                                        .ReverseHitSounds.ElementAtOrDefault(i)
                                        .HasFlag(hitSound)
                                    && !removedSlider
                                        .ReverseHitSounds.ElementAtOrDefault(i)
                                        .HasFlag(hitSound)
                                )
                                    yield return "Added "
                                        + Enum.GetName(typeof(HitObject.HitSounds), hitSound)
                                            ?.ToLower()
                                        + " to reverse #"
                                        + (i + 1)
                                        + ".";

                                if (
                                    !addedSlider
                                        .ReverseHitSounds.ElementAtOrDefault(i)
                                        .HasFlag(hitSound)
                                    && removedSlider
                                        .ReverseHitSounds.ElementAtOrDefault(i)
                                        .HasFlag(hitSound)
                                )
                                    yield return "Removed "
                                        + Enum.GetName(typeof(HitObject.HitSounds), hitSound)
                                            ?.ToLower()
                                        + " from reverse #"
                                        + (i + 1)
                                        + ".";
                            }
                }

                if (addedSlider.StartHitSound != removedSlider.StartHitSound)
                    foreach (
                        HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds))
                    )
                    {
                        if (
                            addedSlider.StartHitSound.HasFlag(hitSound)
                            && !removedSlider.StartHitSound.HasFlag(hitSound)
                        )
                            yield return "Added "
                                + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower()
                                + " to head.";

                        if (
                            !addedSlider.StartHitSound.HasFlag(hitSound)
                            && removedSlider.StartHitSound.HasFlag(hitSound)
                        )
                            yield return "Removed "
                                + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower()
                                + " from head.";
                    }

                if (addedSlider.StartSampleset != removedSlider.StartSampleset)
                    yield return "Head sampleset changed from "
                        + removedSlider.StartSampleset.ToString().ToLower()
                        + " to "
                        + addedSlider.StartSampleset.ToString().ToLower()
                        + ".";

                if (addedSlider.StartAddition != removedSlider.StartAddition)
                    yield return "Head addition changed from "
                        + removedSlider.StartAddition.ToString().ToLower()
                        + " to "
                        + addedSlider.StartAddition.ToString().ToLower()
                        + ".";
            }
            else if (addedObject is Spinner addedSpinner && removedObject is Spinner removedSpinner)
            {
                double addedDuration = addedSpinner.endTime - addedSpinner.time;
                double removedDuration = removedSpinner.endTime - removedSpinner.time;
                if (!addedDuration.AlmostEqual(removedDuration))
                    yield return "End time changed from "
                        + removedSpinner.endTime
                        + " to "
                        + addedSpinner.endTime
                        + ".";
            }
            else if (addedObject is HoldNote addedNote && removedObject is HoldNote removedNote)
            {
                double addedDuration = addedNote.endTime - addedNote.time;
                double removedDuration = removedNote.endTime - removedNote.time;
                if (!addedDuration.AlmostEqual(removedDuration))
                    yield return "End time changed from "
                        + removedNote.endTime
                        + " to "
                        + addedNote.endTime
                        + ".";
            }
        }
    }
}
