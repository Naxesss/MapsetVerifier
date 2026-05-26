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

        // Drift from the estimated global shift carries a stronger penalty than absolute
        // time distance: a "wrong" match with small absolute drift but the wrong shift
        // would otherwise out-compete the correct match across a big section-wide shift.
        private const double ShiftDriftWeight = 0.5;

        private static double GetDistance(TimingLine x, TimingLine y, double globalShift)
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

            // 3. Penalise deviation from the dominant shift, not raw time distance.
            // Without this, DTW would pair an old line to a nearby unrelated new line
            // because the absolute time gap is small, even though the section was shifted
            // by, say, +2009ms and the correct match sits further away.
            double drift = (y.Offset - x.Offset) - globalShift;
            cost += Math.Abs(drift) * ShiftDriftWeight;

            return cost;
        }

        private static List<Tuple<int, int>> AlignTimingLines(
            List<TimingLine> oldList,
            List<TimingLine> newList,
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
                var oldLine = oldList[i - 1];
                for (int j = 1; j <= m; j++)
                {
                    var newLine = newList[j - 1];
                    double timeDiff = Math.Abs(oldLine.Offset - newLine.Offset);

                    if (timeDiff <= maxWindow)
                    {
                        double matchCost = GetDistance(oldLine, newLine, globalShift);
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
                        double matchCost = GetDistance(oldLine, newLine, globalShift);
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

                    // Fallback path (no DTW); pass 0 shift — produces a plain timestamp
                    // and empty suffix, matching the main path's no-shift formatting.
                    var (prefix, suffix) = BuildRemovedStamp(removedLine.Offset, 0);
                    var type = removedLine.Uninherited ? "Uninherited line" : "Inherited line";

                    yield return new DiffInstance(
                        prefix + type + " removed" + suffix + ".",
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

            // Estimate the dominant shift first via a histogram over all candidate pairs.
            // DTW then uses this as a bias so it picks correctly-shifted matches even when
            // one side has many extra inserted lines (e.g. volume ramps without an old
            // counterpart) that would otherwise look like cheap local matches.
            double globalShiftHint = ShiftRansac.EstimateGlobalShift(
                oldTimingLines, newTimingLines,
                l => l.Offset, l => l.Offset,
                (o, n) => o.Uninherited == n.Uninherited,
                5000.0
            );

            // Align old and new timing lines
            var alignment = AlignTimingLines(oldTimingLines, newTimingLines, globalShiftHint, 5000.0);

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

            // Detect the dominant shift via RANSAC voting across all matched pairs.
            // Independent of any hit-object shift: timing can shift without objects shifting
            // (e.g. mp3 offset adjustment on a sparse map) and vice versa.
            var sections = BuildShiftSections(steps, globalShiftHint);

            // Yield diffs for each section
            foreach (var section in sections)
            {
                int matchedCount = section.Steps.Count(s => s.Shift != null && Math.Abs(s.Shift.Value - section.Shift) <= ShiftTolerance);
                bool isSectionShift = Math.Abs(section.Shift) >= 1.0 && matchedCount >= 2;

                if (isSectionShift)
                {
                    var stamp = Timestamp.Get(section.StartTime);
                    var sign = section.Shift > 0 ? "+" : "";

                    yield return new DiffInstance(
                        stamp + $"Section shifted in time by {sign}{section.Shift:0.##} ms ({matchedCount} timing points).",
                        Section,
                        DiffType.Changed,
                        new List<string>(),
                        snapshotCreationDate
                    );
                }

                // Emit residuals flat (separate entries, never nested inside the shift summary):
                // - Net-added / net-removed lines
                // - Matched lines whose shift drifts beyond +/-2ms of the section shift
                // - Matched lines with non-time property changes (volume, sampleset, kiai, BPM, SV, ...)
                // Matched lines that are purely on-shift with no other change are suppressed.
                foreach (var s in section.Steps)
                {
                    var typeObj = s.NewLine?.Uninherited == true || s.OldLine?.Uninherited == true ? "Uninherited line" : "Inherited line";

                    if (s.OldLine != null && s.NewLine == null)
                    {
                        // Display orphan removals at the dominant-shifted time so they
                        // appear next to surviving objects in the new timeline, with the
                        // original pre-shift timestamp appended in parentheses for cross-
                        // reference against the old beatmap.
                        var (prefix, suffix) = BuildRemovedStamp(s.OldLine.Offset, globalShiftHint);
                        yield return new DiffInstance(
                            prefix + typeObj + " removed" + suffix + ".",
                            Section,
                            DiffType.Removed,
                            new List<string>(),
                            snapshotCreationDate
                        );
                    }
                    else if (s.OldLine == null && s.NewLine != null)
                    {
                        var stampObj = Timestamp.Get(s.NewLine.Offset);
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
                        var stampObj = Timestamp.Get(s.NewLine.Offset);
                        var changes = GetChanges(s.NewLine, s.OldLine, beatmap).ToList();
                        double localShift = s.NewLine.Offset - s.OldLine.Offset;
                        double driftFromSection = localShift - section.Shift;
                        double snapTolerance = GetSnapTolerance(beatmap, s.NewLine.Offset);
                        bool aligned = Math.Abs(driftFromSection) <= snapTolerance;

                        if (isSectionShift)
                        {
                            // Drift beyond a 1/32 beat at the local BPM — surface it. Sub-snap
                            // jitter is absorbed into the section shift.
                            if (!aligned)
                            {
                                var localSign = driftFromSection > 0 ? "+" : "";
                                changes.Insert(0, $"Time drifts from section shift by {localSign}{driftFromSection:0.##} ms (line shift {localShift:0.##} ms).");
                            }
                        }
                        else if (Math.Abs(localShift) >= 1.0)
                        {
                            var localSign = localShift > 0 ? "+" : "";
                            changes.Insert(0, $"Time changed from {s.OldLine.Offset} ms to {s.NewLine.Offset} ms ({localSign}{localShift:0.##} ms).");
                        }

                        if (changes.Count > 0)
                        {
                            // Always keep the line type in the title so single-change
                            // entries (e.g. only a volume diff) stay obviously tied to a
                            // timing line rather than floating standalone.
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

        // RANSAC needs at least 2 matched pairs voting for the same shift to call it a section.
        private const int MinShiftInliers = 2;

        // Per-step drift report uses 1/32 of the active beat as the tolerance, so sub-snap
        // jitter is absorbed into the section shift while real snap-level drift surfaces.
        // Falls back to ShiftTolerance if no uninherited line is active.
        private static double GetSnapTolerance(Beatmap beatmap, double time)
        {
            var uninherited = beatmap.GetTimingLine<UninheritedLine>(time);
            if (uninherited == null) return ShiftTolerance;
            return uninherited.msPerBeat / 32.0;
        }

        // Returns (prefix, suffix) for an orphan-removed entry. Prefix carries the
        // (shifted, if any) timestamp; suffix carries the "(originally <old>)"
        // cross-reference appended after the message body.
        // No shift -> suffix is empty (would just repeat the primary stamp).
        private static (string prefix, string suffix) BuildRemovedStamp(double oldTime, double shift)
        {
            if (Math.Abs(shift) < 1.0)
                return (Timestamp.Get(oldTime), "");
            string oldTrim = Timestamp.Get(oldTime).TrimEnd(' ', '-');
            return (Timestamp.Get(oldTime + shift), $" (originally {oldTrim})");
        }

        // Cluster every matched step's shift greedily within +/-ShiftTolerance. Any cluster
        // with >= MinShiftInliers becomes its own section (so e.g. seven lines all shifted
        // by -15ms collapse into a single "Section shifted by -15 ms" entry). Singleton
        // shifts, unmatched steps (add/remove), and the zero-shift "matched-but-unchanged"
        // group become per-step sections that the residual loop handles individually.
        private static List<ShiftSection> BuildShiftSections(List<UnifiedStep> steps, double globalShiftHint)
        {
            var clusters = new List<List<UnifiedStep>>();
            var unmatched = new List<UnifiedStep>();

            foreach (var step in steps)
            {
                if (!step.Shift.HasValue)
                {
                    unmatched.Add(step);
                    continue;
                }

                long shift = (long)System.Math.Round(step.Shift.Value);
                var existing = clusters.FirstOrDefault(c =>
                    System.Math.Abs((long)System.Math.Round(c[0].Shift!.Value) - shift) <= ShiftTolerance);
                if (existing != null) existing.Add(step);
                else clusters.Add(new List<UnifiedStep> { step });
            }

            var sections = new List<ShiftSection>();

            foreach (var cluster in clusters)
            {
                // Use the rounded shift of the cluster anchor as the canonical value; all
                // cluster members are within +/-ShiftTolerance so any choice is fine.
                long shift = (long)System.Math.Round(cluster[0].Shift!.Value);
                bool isShift = System.Math.Abs(shift) >= 1 && cluster.Count >= MinShiftInliers;

                if (isShift)
                {
                    var sec = new ShiftSection
                    {
                        Shift = shift,
                        StartTime = cluster.Min(GetStepTime),
                        EndTime = cluster.Max(GetStepTime),
                    };
                    sec.Steps.AddRange(cluster);
                    sections.Add(sec);
                }
                else
                {
                    // Either zero-shift (no change to time) or a single isolated shift; let
                    // the residual loop emit each step on its own.
                    foreach (var step in cluster)
                    {
                        var sec = new ShiftSection
                        {
                            Shift = step.Shift ?? 0.0,
                            StartTime = GetStepTime(step),
                            EndTime = GetStepTime(step),
                        };
                        sec.Steps.Add(step);
                        sections.Add(sec);
                    }
                }
            }

            foreach (var step in unmatched)
            {
                // Sort by displayed time, not raw step time, so orphan removed entries
                // (rendered at OldLine.Offset + globalShiftHint) interleave correctly with
                // orphan added entries (rendered at NewLine.Offset).
                double displayTime = step.NewLine != null
                    ? step.NewLine.Offset
                    : step.OldLine!.Offset + globalShiftHint;
                var sec = new ShiftSection
                {
                    Shift = 0.0,
                    StartTime = displayTime,
                    EndTime = displayTime,
                };
                sec.Steps.Add(step);
                sections.Add(sec);
            }

            return sections.OrderBy(s => s.StartTime).ToList();
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
