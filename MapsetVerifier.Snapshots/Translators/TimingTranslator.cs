using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using MathNet.Numerics;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class TimingTranslator : DiffTranslator
    {
        public override string Section => "TimingPoints";
        public override string TranslatedSection => "Timing";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            var addedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();
            var removedTimingLines = new List<Tuple<DiffInstance, TimingLine>>();

            foreach (var diff in diffs)
            {
                TimingLine? timingLine = null;

                try
                {
                    timingLine = new TimingLine(diff.Diff.Split(','), null!);
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
                        removedTimingLines.Add(new Tuple<DiffInstance, TimingLine>(diff, timingLine));
                }
                else
                    // Shows the raw .osu line change.
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

                foreach (var removedLine in removedTimingLines.Select(tuple => tuple.Item2).ToList())
                {
                    if (!addedLine.Offset.AlmostEqual(removedLine.Offset))
                        continue;

                    var removedType = removedLine.Uninherited ? "Uninherited line" : "Inherited line";

                    if (type != removedType)
                        continue;

                    var changes = new List<string>();

                    if (addedLine.Kiai != removedLine.Kiai)
                        changes.Add("Kiai changed from " + (removedLine.Kiai ? "enabled" : "disabled") + " to " + (addedLine.Kiai ? "enabled" : "disabled") + ".");

                    if (addedLine.Meter != removedLine.Meter)
                        changes.Add("Timing signature changed from " + removedLine.Meter + "/4" + " to " + addedLine.Meter + "/4.");

                    if (addedLine.Sampleset != removedLine.Sampleset)
                        changes.Add("Sampleset changed from " + removedLine.Sampleset.ToString().ToLower() + " to " + addedLine.Sampleset.ToString().ToLower() + ".");

                    if (addedLine.CustomIndex != removedLine.CustomIndex)
                        changes.Add("Custom sampleset index changed from " + removedLine.CustomIndex.ToString().ToLower() + " to " + addedLine.CustomIndex.ToString().ToLower() + ".");

                    if (!addedLine.Volume.AlmostEqual(removedLine.Volume))
                        changes.Add("Volume changed from " + removedLine.Volume + " to " + addedLine.Volume + ".");

                    if (type == "Uninherited line")
                    {
                        var addedUninherited = new UninheritedLine(addedLine.Code.Split(','), null!);
                        var removedUninherited = new UninheritedLine(removedLine.Code.Split(','), null!);

                        if (!addedUninherited.bpm.AlmostEqual(removedUninherited.bpm))
                            changes.Add("BPM changed from " + removedUninherited.bpm + " to " + addedUninherited.bpm + ".");
                    }
                    else if (!addedLine.SvMult.AlmostEqual(removedLine.SvMult))
                    {
                        changes.Add("Slider velocity multiplier changed from " + removedLine.SvMult + " to " + addedLine.SvMult + ".");
                    }

                    if (changes.Count == 1)
                        yield return new DiffInstance(stamp + changes[0], Section, DiffType.Changed, new List<string>(), addedDiff.SnapshotCreationDate);
                    else if (changes.Count > 1)
                        yield return new DiffInstance(stamp + type + " changed.", Section, DiffType.Changed, changes, addedDiff.SnapshotCreationDate);

                    found = true;
                    removedTimingLines.RemoveAll(tuple => tuple.Item2.Code == removedLine.Code);
                }

                if (!found)
                    yield return new DiffInstance(stamp + type + " added.", Section, DiffType.Added, new List<string>(), addedDiff.SnapshotCreationDate);
            }

            foreach (var removedTuple in removedTimingLines)
            {
                var removedDiff = removedTuple.Item1;
                var removedLine = removedTuple.Item2;

                var stamp = Timestamp.Get(removedLine.Offset);
                var type = removedLine.Uninherited ? "Uninherited line" : "Inherited line";

                yield return new DiffInstance(stamp + type + " removed.", Section, DiffType.Removed, new List<string>(), removedDiff.SnapshotCreationDate);
            }
        }
    }
}