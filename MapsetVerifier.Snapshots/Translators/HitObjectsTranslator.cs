using System.Text;
using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
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

        private static double GetDistance(HitObject x, HitObject y)
        {
            // Hybrid distance function to pair up correct objects:
            // Combines spatial, type, and temporal info to prevent beat-snapping mismatches.
            double cost = 0;

            if (x.GetObjectType() != y.GetObjectType())
                cost += 500.0; // Strong penalty for different types, but still allows matching if they are close

            // 1. Spatial distance using log scale to reduce the penalty of large movements
            double posDist = Vector2.Distance(x.Position, y.Position);
            cost += Math.Log(1.0 + posDist) * 15.0;

            // 2. Hitsounds mismatch: completely removed for DTW alignment

            // 3. Extras mismatch
            if (x.sampleset != y.sampleset) cost += 10.0;
            if (x.addition != y.addition) cost += 10.0;
            if (x.customIndex != y.customIndex) cost += 10.0;
            if (x.volume != y.volume) cost += 10.0;
            if (x.filename != y.filename) cost += 20.0;

            // 4. Object-specific properties mismatch (excluding hitsounds)
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

            // 5. Light time penalty to prefer closer in time matches if all else is equal
            cost += Math.Abs(x.time - y.time) * 0.05;

            // 6. Give higher preference to exact matches (excluding hitsounds)
            bool isExactMatch = x.GetObjectType() == y.GetObjectType() &&
                                x.Position == y.Position &&
                                x.sampleset == y.sampleset &&
                                x.addition == y.addition &&
                                (x.customIndex ?? 0) == (y.customIndex ?? 0) &&
                                (x.volume ?? 0) == (y.volume ?? 0) &&
                                x.filename == y.filename;

            if (x is Slider xs && y is Slider ys)
            {
                isExactMatch = isExactMatch &&
                               xs.CurveType == ys.CurveType &&
                               xs.EdgeAmount == ys.EdgeAmount &&
                               xs.NodePositions.Count == ys.NodePositions.Count;
            }

            if (!isExactMatch)
            {
                cost += 20.0; // Flat penalty for not being an exact match
            }

            return cost;
        }

        private static List<Tuple<int, int>> AlignHitObjects(
            List<HitObject> oldList,
            List<HitObject> newList,
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
                        double matchCost = GetDistance(oldObj, newObj);
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
                        double matchCost = GetDistance(oldObj, newObj);
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

                    var stamp = Timestamp.Get(removedObject.time);
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

            // Get shift sections from helper
            var sections = GetShiftSections(beatmap, oldCode, newCode);

            // Yield diffs for each section
            foreach (var section in sections)
            {
                int matchedCount = section.Steps.Count(s => s.Shift != null && Math.Abs(s.Shift.Value - section.Shift) <= 2.0);

                if (section.IsSectionShift)
                {
                    var stamp = Timestamp.Get(section.StartTime);
                    var sign = section.Shift > 0 ? "+" : "";
                    var details = new List<string>();

                    foreach (var s in section.Steps)
                    {
                        var timeStr = Timestamp.Get(GetStepTime(s));
                        var typeStr = s.NewObj?.GetObjectType() ?? s.OldObj?.GetObjectType() ?? "Object";

                        if (s.OldObj != null && s.NewObj == null)
                        {
                            // Real Removal (No counterpart in shifted space)
                            details.Add($"{timeStr}{typeStr} was removed.");
                        }
                        else if (s.OldObj == null && s.NewObj != null)
                        {
                            // Real Addition (No counterpart in shifted space)
                            details.Add($"{timeStr}{typeStr} was added.");
                        }
                        else if (s.OldObj != null && s.NewObj != null)
                        {
                            var changes = GetChanges(s.NewObj, s.OldObj, beatmap).ToList();
                            double localShift = s.NewObj.time - s.OldObj.time;

                            if (Math.Abs(localShift - section.Shift) > 2.0)
                            {
                                var diffVal = localShift - section.Shift;
                                var localSign = diffVal > 0 ? "+" : "";
                                changes.Insert(0, $"Time changed individually by {localSign}{diffVal:0.##} ms (total shift {localShift:0.##} ms).");
                            }

                            if (changes.Count > 0)
                            {
                                if (changes.Count == 1)
                                    details.Add($"{timeStr}{changes[0]}");
                                else
                                    details.Add($"{timeStr}{typeStr} changed: {string.Join(" ", changes)}");
                            }
                        }
                    }

                    yield return new DiffInstance(
                        stamp + $"Section shifted in time by {sign}{section.Shift:0.##} ms ({matchedCount} objects).",
                        Section,
                        DiffType.Changed,
                        details,
                        snapshotCreationDate
                    );
                }
                else
                {
                    // Treat as individual alignments
                    foreach (var s in section.Steps)
                    {
                        var stampObj = Timestamp.Get(GetStepTime(s));
                        var typeObj = s.NewObj?.GetObjectType() ?? s.OldObj?.GetObjectType() ?? "Object";

                        if (s.OldObj != null && s.NewObj == null)
                        {
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
                            var changes = GetChanges(s.NewObj, s.OldObj, beatmap).ToList();
                            double localShift = s.NewObj.time - s.OldObj.time;

                            if (Math.Abs(localShift) >= 1.0)
                            {
                                var localSign = localShift > 0 ? "+" : "";
                                changes.Insert(0, $"Time changed from {s.OldObj.time} ms to {s.NewObj.time} ms ({localSign}{localShift:0.##} ms).");
                            }

                            if (changes.Count > 0)
                            {
                                if (changes.Count == 1)
                                {
                                    yield return new DiffInstance(
                                        stampObj + changes[0],
                                        Section,
                                        DiffType.Changed,
                                        new List<string>(),
                                        snapshotCreationDate
                                    );
                                }
                                else
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
            }
        }

        public static List<ShiftSection> GetShiftSections(
            Beatmap beatmap,
            string oldCode,
            string? newCode
        )
        {
            // Parse old hit objects
            var oldHitObjects = ParseHitObjectsFromCode(oldCode, beatmap);
            var newHitObjects = !string.IsNullOrEmpty(newCode)
                ? ParseHitObjectsFromCode(newCode, beatmap)
                : beatmap.HitObjects;

            // Align old and new hit objects using DTW (Needleman-Wunsch with window)
            var alignment = AlignHitObjects(oldHitObjects, newHitObjects, 5000.0);

            // Convert alignment into UnifiedSteps
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

            // Group steps into ShiftSections based on shift tolerance (2ms) and lookahead merging
            var sections = new List<ShiftSection>();
            int i = 0;
            while (i < steps.Count)
            {
                var step = steps[i];
                if (step.Shift == null)
                {
                    var section = new ShiftSection
                    {
                        Shift = 0,
                        StartTime = GetStepTime(step),
                        EndTime = GetStepTime(step)
                    };
                    section.Steps.Add(step);
                    sections.Add(section);
                    i++;
                    continue;
                }

                double targetShift = step.Shift.Value;
                var currentSection = new ShiftSection
                {
                    Shift = targetShift,
                    StartTime = GetStepTime(step),
                    EndTime = GetStepTime(step)
                };
                currentSection.Steps.Add(step);
                i++;

                while (i < steps.Count)
                {
                    bool foundResume = false;
                    int resumeIndex = -1;
                    int unmatchedCount = 0;

                    for (int k = i; k < steps.Count; k++)
                    {
                        var aheadStep = steps[k];
                        if (aheadStep.Shift == null || Math.Abs(aheadStep.Shift.Value - targetShift) > 2.0)
                        {
                            unmatchedCount++;
                        }
                        else
                        {
                            foundResume = true;
                            resumeIndex = k;
                            break;
                        }

                        if (unmatchedCount > 20)
                            break;
                    }

                    if (foundResume)
                    {
                        for (int k = i; k <= resumeIndex; k++)
                        {
                            currentSection.Steps.Add(steps[k]);
                            currentSection.EndTime = GetStepTime(steps[k]);
                        }
                        i = resumeIndex + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                sections.Add(currentSection);
            }

            foreach (var section in sections)
            {
                int matchedCount = section.Steps.Count(s => s.Shift != null && Math.Abs(s.Shift.Value - section.Shift) <= 2.0);
                section.IsSectionShift = Math.Abs(section.Shift) >= 1.0 && matchedCount >= 5;
            }

            return sections;
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
