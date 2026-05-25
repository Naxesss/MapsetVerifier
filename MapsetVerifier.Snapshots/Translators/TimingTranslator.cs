using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using MathNet.Numerics;
using Serilog;
using System.Text;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class TimingTranslator : DiffTranslator
    {
        public override string Section => "TimingPoints";
        public override string TranslatedSection => "Timing";

        private class ShiftSection
        {
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public double Shift { get; set; }
            public List<UnifiedStep> Steps { get; } = new List<UnifiedStep>();
        }

        private class UnifiedStep
        {
            public TimingLine? OldLine { get; set; }
            public TimingLine? NewLine { get; set; }
            public double? Shift => (OldLine != null && NewLine != null) ? (double?)(NewLine.Offset - OldLine.Offset) : null;
        }

        private static double GetStepTime(UnifiedStep step)
        {
            if (step.NewLine != null) return step.NewLine.Offset;
            if (step.OldLine != null) return step.OldLine.Offset;
            return 0;
        }

        private static HitObjectsTranslator.ShiftSection? GetClosestHitObjectSection(
            List<HitObjectsTranslator.ShiftSection> sections,
            double time
        )
        {
            if (sections == null || sections.Count == 0)
                return null;

            foreach (var sec in sections)
            {
                if (time >= sec.StartTime && time <= sec.EndTime)
                    return sec;
            }

            HitObjectsTranslator.ShiftSection? closest = null;
            double minDistance = double.MaxValue;
            foreach (var sec in sections)
            {
                double dist = 0;
                if (time < sec.StartTime)
                    dist = sec.StartTime - time;
                else if (time > sec.EndTime)
                    dist = time - sec.EndTime;

                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = sec;
                }
            }

            return closest;
        }

        private static List<TimingLine> ParseTimingLinesFromCode(string code, Beatmap beatmap)
        {
            var timingLines = new List<TimingLine>();
            var lines = code.Replace("\r", "").Split('\n');
            bool inTimingSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inTimingSection = (trimmed == "[TimingPoints]");
                    continue;
                }

                if (inTimingSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    try
                    {
                        var args = trimmed.Split(',');
                        var timingLine = TimingLine.IsUninherited(args)
                            ? new UninheritedLine(args, beatmap)
                            : (TimingLine)new InheritedLine(args, beatmap);
                        timingLines.Add(timingLine);
                    }
                    catch
                    {
                        // Ignore parse errors
                    }
                }
            }

            return timingLines.OrderBy(line => line.Offset).ThenBy(line => line is InheritedLine).ToList();
        }

        private static double GetDistance(TimingLine x, TimingLine y)
        {
            double cost = 0;

            if (x.Uninherited != y.Uninherited)
                cost += 500.0;

            // 1. Check basic properties mismatch
            if (x.Kiai != y.Kiai) cost += 50.0;
            if (x.Meter != y.Meter) cost += 50.0;
            if (x.Sampleset != y.Sampleset) cost += 30.0;
            if (x.CustomIndex != y.CustomIndex) cost += 20.0;
            if (!x.Volume.AlmostEqual(y.Volume)) cost += 20.0;

            // 2. Specific properties: Bpm for uninherited, SV for inherited
            if (x.Uninherited && y.Uninherited)
            {
                var xUninherited = x as UninheritedLine;
                var yUninherited = y as UninheritedLine;
                if (xUninherited != null && yUninherited != null)
                {
                    cost += Math.Abs(xUninherited.bpm - yUninherited.bpm) * 2.0;
                }
            }
            else if (!x.Uninherited && !y.Uninherited)
            {
                cost += Math.Abs(x.SvMult - y.SvMult) * 100.0;
            }

            // 3. Temporal distance (light penalty)
            cost += Math.Abs(x.Offset - y.Offset) * 0.05;

            return cost;
        }

        private static List<Tuple<int, int>> AlignTimingLines(
            List<TimingLine> oldList,
            List<TimingLine> newList,
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
                var oldLine = oldList[i - 1];
                for (int j = 1; j <= m; j++)
                {
                    var newLine = newList[j - 1];
                    double timeDiff = Math.Abs(oldLine.Offset - newLine.Offset);

                    if (timeDiff <= maxWindow)
                    {
                        double matchCost = GetDistance(oldLine, newLine);
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
                    var oldLine = oldList[currI - 1];
                    var newLine = newList[currJ - 1];
                    double timeDiff = Math.Abs(oldLine.Offset - newLine.Offset);

                    if (timeDiff <= maxWindow)
                    {
                        double matchCost = GetDistance(oldLine, newLine);
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
                var addedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();
                var removedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();

                foreach (var diff in diffsList)
                {
                    TimingLine? timingLine = null;

                    try
                    {
                        timingLine = new TimingLine(diff.Diff.Split(','), beatmap);
                    }
                    catch
                    {
                        // Failing to parse a changed line shouldn't stop it from showing.
                    }

                    if (timingLine != null)
                    {
                        if (diff.DiffType == DiffType.Added)
                            addedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                        else
                            removedTimingLines.Add(
                                new Tuple<DiffInstance, TimingLine>(diff, timingLine)
                            );
                    }
                    else
                    {
                        yield return diff;
                    }
                }

                foreach (var addedTuple in addedTimingLines)
                {
                    var addedDiff = addedTuple.Item1;
                    var addedLine = addedTuple.Item2;

                    var stamp = Timestamp.Get(addedLine.Offset);
                    var type = addedLine.Uninherited ? "Uninherited line" : "Inherited line";

                    var found = false;

                    foreach (
                        var removedLine in removedTimingLines.Select(tuple => tuple.Item2).ToList()
                    )
                    {
                        if (!addedLine.Offset.AlmostEqual(removedLine.Offset))
                            continue;

                        var removedType = removedLine.Uninherited
                            ? "Uninherited line"
                            : "Inherited line";

                        if (type != removedType)
                            continue;

                        var changes = GetChanges(addedLine, removedLine, beatmap).ToList();

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
                        removedTimingLines.RemoveAll(tuple => tuple.Item2.Code == removedLine.Code);
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

                foreach (var removedTuple in removedTimingLines)
                {
                    var removedDiff = removedTuple.Item1;
                    var removedLine = removedTuple.Item2;

                    var stamp = Timestamp.Get(removedLine.Offset);
                    var type = removedLine.Uninherited ? "Uninherited line" : "Inherited line";

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

            // Parse old and new timing lines
            var oldTimingLines = ParseTimingLinesFromCode(oldCode, beatmap);
            var newTimingLines = !string.IsNullOrEmpty(newCode)
                ? ParseTimingLinesFromCode(newCode, beatmap)
                : beatmap.TimingLines;

            // Align old and new timing lines
            var alignment = AlignTimingLines(oldTimingLines, newTimingLines, 5000.0);

            // Convert alignment into UnifiedSteps
            var steps = new List<UnifiedStep>();
            foreach (var step in alignment)
            {
                var unified = new UnifiedStep();
                if (step.Item1 != -1)
                    unified.OldLine = oldTimingLines[step.Item1];
                if (step.Item2 != -1)
                    unified.NewLine = newTimingLines[step.Item2];
                steps.Add(unified);
            }

            // Get hit objects shift sections
            List<HitObjectsTranslator.ShiftSection>? hitObjectSections = null;
            try
            {
                hitObjectSections = HitObjectsTranslator.GetShiftSections(beatmap, oldCode, newCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not compute hit object shifts for timing translator");
            }

            // Group steps into ShiftSections based on hitobjects shift sections (or fallback to default logic)
            var sections = new List<ShiftSection>();
            if (hitObjectSections != null && hitObjectSections.Count > 0)
            {
                var groups = steps.GroupBy(step => GetClosestHitObjectSection(hitObjectSections, GetStepTime(step)));

                foreach (var group in groups)
                {
                    var hitObjectSec = group.Key;
                    if (hitObjectSec == null)
                        continue;

                    if (hitObjectSec.IsSectionShift)
                    {
                        var sec = new ShiftSection
                        {
                            Shift = hitObjectSec.Shift,
                            StartTime = group.Min(GetStepTime),
                            EndTime = group.Max(GetStepTime)
                        };
                        sec.Steps.AddRange(group);
                        sections.Add(sec);
                    }
                    else
                    {
                        foreach (var step in group)
                        {
                            var sec = new ShiftSection
                            {
                                Shift = 0,
                                StartTime = GetStepTime(step),
                                EndTime = GetStepTime(step)
                            };
                            sec.Steps.Add(step);
                            sections.Add(sec);
                        }
                    }
                }

                sections = sections.OrderBy(s => s.StartTime).ToList();
            }
            else
            {
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
            }

            // Yield diffs for each section
            foreach (var section in sections)
            {
                int matchedCount = section.Steps.Count(s => s.Shift != null && Math.Abs(s.Shift.Value - section.Shift) <= 2.0);
                bool isSectionShift = Math.Abs(section.Shift) >= 1.0 && matchedCount >= 2;

                if (isSectionShift)
                {
                    var stamp = Timestamp.Get(section.StartTime);
                    var sign = section.Shift > 0 ? "+" : "";
                    var details = new List<string>();

                    foreach (var s in section.Steps)
                    {
                        var timeStr = Timestamp.Get(GetStepTime(s));
                        var typeStr = s.NewLine?.Uninherited == true || s.OldLine?.Uninherited == true ? "Uninherited line" : "Inherited line";

                        if (s.OldLine != null && s.NewLine == null)
                        {
                            details.Add($"{timeStr}{typeStr} was removed.");
                        }
                        else if (s.OldLine == null && s.NewLine != null)
                        {
                            details.Add($"{timeStr}{typeStr} was added.");
                        }
                        else if (s.OldLine != null && s.NewLine != null)
                        {
                            var changes = GetChanges(s.NewLine, s.OldLine, beatmap).ToList();
                            double localShift = s.NewLine.Offset - s.OldLine.Offset;

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
                        stamp + $"Section shifted in time by {sign}{section.Shift:0.##} ms ({matchedCount} timing points).",
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
                        var typeObj = s.NewLine?.Uninherited == true || s.OldLine?.Uninherited == true ? "Uninherited line" : "Inherited line";

                        if (s.OldLine != null && s.NewLine == null)
                        {
                            yield return new DiffInstance(
                                stampObj + typeObj + " removed.",
                                Section,
                                DiffType.Removed,
                                new List<string>(),
                                snapshotCreationDate
                            );
                        }
                        else if (s.OldLine == null && s.NewLine != null)
                        {
                            yield return new DiffInstance(
                                stampObj + typeObj + " added.",
                                Section,
                                DiffType.Added,
                                new List<string>(),
                                snapshotCreationDate
                            );
                        }
                        else if (s.OldLine != null && s.NewLine != null)
                        {
                            var changes = GetChanges(s.NewLine, s.OldLine, beatmap).ToList();
                            double localShift = s.NewLine.Offset - s.OldLine.Offset;

                            if (Math.Abs(localShift) >= 1.0)
                            {
                                var localSign = localShift > 0 ? "+" : "";
                                changes.Insert(0, $"Time changed from {s.OldLine.Offset} ms to {s.NewLine.Offset} ms ({localSign}{localShift:0.##} ms).");
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

        private static IEnumerable<string> GetChanges(
            TimingLine addedLine,
            TimingLine removedLine,
            Beatmap beatmap
        )
        {
            if (addedLine.Kiai != removedLine.Kiai)
                yield return "Kiai changed from "
                    + (removedLine.Kiai ? "enabled" : "disabled")
                    + " to "
                    + (addedLine.Kiai ? "enabled" : "disabled")
                    + ".";

            if (addedLine.Meter != removedLine.Meter)
                yield return "Timing signature changed from "
                    + removedLine.Meter
                    + "/4"
                    + " to "
                    + addedLine.Meter
                    + "/4.";

            if (addedLine.Sampleset != removedLine.Sampleset)
                yield return "Sampleset changed from "
                    + removedLine.Sampleset.ToString().ToLower()
                    + " to "
                    + addedLine.Sampleset.ToString().ToLower()
                    + ".";

            if (addedLine.CustomIndex != removedLine.CustomIndex)
                yield return "Custom sampleset index changed from "
                    + removedLine.CustomIndex.ToString().ToLower()
                    + " to "
                    + addedLine.CustomIndex.ToString().ToLower()
                    + ".";

            if (!addedLine.Volume.AlmostEqual(removedLine.Volume))
                yield return "Volume changed from "
                    + removedLine.Volume
                    + " to "
                    + addedLine.Volume
                    + ".";

            if (addedLine.Uninherited && removedLine.Uninherited)
            {
                var addedUninherited = new UninheritedLine(
                    addedLine.Code.Split(','),
                    beatmap
                );
                var removedUninherited = new UninheritedLine(
                    removedLine.Code.Split(','),
                    beatmap
                );

                if (!addedUninherited.bpm.AlmostEqual(removedUninherited.bpm))
                    yield return "BPM changed from "
                        + removedUninherited.bpm
                        + " to "
                        + addedUninherited.bpm
                        + ".";
            }
            else if (!addedLine.Uninherited && !removedLine.Uninherited && !addedLine.SvMult.AlmostEqual(removedLine.SvMult))
            {
                yield return "Slider velocity multiplier changed from "
                    + removedLine.SvMult
                    + " to "
                    + addedLine.SvMult
                    + ".";
            }
        }
    }
}
