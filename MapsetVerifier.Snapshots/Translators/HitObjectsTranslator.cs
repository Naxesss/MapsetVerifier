using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using MathNet.Numerics;
using osu.Game.Rulesets.Catch.Objects;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class HitObjectsTranslator : DiffTranslator
    {
        public override string Section => "HitObjects";
        public override string TranslatedSection => "Hit Objects";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            var addedHitObjects = new List<Tuple<DiffInstance, HitObject>>();
            var removedHitObjects = new List<Tuple<DiffInstance, HitObject>>();

            foreach (var diff in diffs)
            {
                HitObject? hitObject = null;

                try
                {
                    hitObject = new HitObject(diff.Diff.Split(','), null!);
                }
                catch
                {
                    // Cannot yield in a catch clause, so checks for null in the following statement instead.
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

                        var changes = GetChanges(addedObject, removedObject).ToList();

                        if (changes.Count == 1)
                            yield return new DiffInstance(stamp + changes[0], Section, DiffType.Changed, new List<string>(), addedDiff.SnapshotCreationDate);
                        else if (changes.Count > 1)
                            yield return new DiffInstance(stamp + type + " changed.", Section, DiffType.Changed, changes, addedDiff.SnapshotCreationDate);

                        found = true;
                        var o = removedObject;
                        removedHitObjects.RemoveAll(tuple => tuple.Item2.code == o.code);
                    }

                // First check all following objects and see what happened to them. Once we get to a point where
                // time and properties match up again, we can go back here and check these until that point.
                // If the only difference is time, the object was most likely just offset.
                if (!found)
                    yield return new DiffInstance(stamp + type + " added.", Section, DiffType.Added, new List<string>(), addedDiff.SnapshotCreationDate);
            }

            foreach (var removedTuple in removedHitObjects)
            {
                var removedDiff = removedTuple.Item1;
                var removedObject = removedTuple.Item2;

                var stamp = Timestamp.Get(removedObject.time);
                var type = removedObject.GetObjectType();

                yield return new DiffInstance(stamp + type + " removed.", Section, DiffType.Removed, new List<string>(), removedDiff.SnapshotCreationDate);
            }
        }

        private IEnumerable<string> GetChanges(HitObject addedObject, HitObject removedObject)
        {
            if (addedObject.Position != removedObject.Position)
                yield return "Moved from (" + removedObject.Position.X + "; " + removedObject.Position.Y + ") to (" + addedObject.Position.X + "; " + addedObject.Position.Y + ").";

            if (addedObject.hitSound != removedObject.hitSound)
                foreach (HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds)))
                {
                    if (addedObject.HasHitSound(hitSound) && !removedObject.HasHitSound(hitSound))
                        yield return "Added " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + ".";

                    if (!addedObject.HasHitSound(hitSound) && removedObject.HasHitSound(hitSound))
                        yield return "Removed " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + ".";
                }

            if (addedObject.sampleset != removedObject.sampleset)
                yield return "Sampleset changed from " + removedObject.sampleset.ToString().ToLower() + " to " + addedObject.sampleset.ToString().ToLower() + ".";

            if (addedObject.addition != removedObject.addition)
                yield return "Addition changed from " + removedObject.addition.ToString().ToLower() + " to " + addedObject.addition.ToString().ToLower() + ".";

            if ((addedObject.customIndex ?? 0) != (removedObject.customIndex ?? 0))
                yield return "Custom sampleset index override changed from " + (removedObject.customIndex?.ToString() ?? "default") + " to " + (addedObject.customIndex?.ToString() ?? "default") + ".";

            if (addedObject.type.HasFlag(HitObject.Types.NewCombo) && !removedObject.type.HasFlag(HitObject.Types.NewCombo))
                yield return "Added new combo.";

            if (!addedObject.type.HasFlag(HitObject.Types.NewCombo) && removedObject.type.HasFlag(HitObject.Types.NewCombo))
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
                yield return "Changed skipped combo amount from " + removedComboSkip + " to " + addedComboSkip + ".";

            if (addedObject.filename != removedObject.filename)
                yield return "Hit sound filename changed from " + removedObject.filename + " to " + addedObject.filename + ".";

            if (addedObject.volume != removedObject.volume)
                yield return "Hit sound volume changed from " + (removedObject.volume?.ToString() ?? "inherited") + " to " + (addedObject.volume?.ToString() ?? "inherited") + ".";

            var type = addedObject.GetObjectType();

            if (type == "Slider")
            {
                var addedSlider = new Slider(addedObject.code.Split(','), null!);
                var removedSlider = new Slider(removedObject.code.Split(','), null!);

                if (addedSlider.CurveType != removedSlider.CurveType)
                    yield return "Curve type changed from " + removedSlider.CurveType + " to " + addedSlider.CurveType + ".";

                if (addedSlider.EdgeAmount != removedSlider.EdgeAmount)
                    yield return "Reverse amount changed from " + (removedSlider.EdgeAmount - 1) + " to " + (addedSlider.EdgeAmount - 1) + ".";

                if (addedSlider.EndSampleset != removedSlider.EndSampleset)
                    yield return "Tail sampleset changed from " + removedSlider.EndSampleset.ToString().ToLower() + " to " + addedSlider.EndSampleset.ToString().ToLower() + ".";

                if (addedSlider.EndAddition != removedSlider.EndAddition)
                    yield return "Tail addition changed from " + removedSlider.EndAddition.ToString().ToLower() + " to " + addedSlider.EndAddition.ToString().ToLower() + ".";

                if (addedSlider.EndHitSound != removedSlider.EndHitSound)
                    foreach (HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds)))
                    {
                        if (addedSlider.HasHitSound(hitSound) && !removedSlider.HasHitSound(hitSound))
                            yield return "Added " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " to tail.";

                        if (!addedSlider.HasHitSound(hitSound) && removedSlider.HasHitSound(hitSound))
                            yield return "Removed " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " from tail.";
                    }

                if (!addedSlider.PixelLength.AlmostEqual(removedSlider.PixelLength))
                    yield return "Pixel length changed from " + removedSlider.PixelLength + " to " + addedSlider.PixelLength + ".";

                if (addedSlider.NodePositions.Count == removedSlider.NodePositions.Count)
                {
                    // The first node is the start, which we already checked.
                    for (var i = 1; i < addedSlider.NodePositions.Count; ++i)
                        if (addedSlider.NodePositions[i] != removedSlider.NodePositions[i])
                            yield return "Node " + (i + 1) + " moved from (" + removedSlider.NodePositions[i].X + "; " + removedSlider.NodePositions[i].Y + ") to (" + addedSlider.NodePositions[i].X + "; " + addedSlider.NodePositions[i].Y + ").";
                }
                else
                {
                    yield return "Node count changed from " + removedSlider.NodePositions.Count + " to " + addedSlider.NodePositions.Count + " (possibly positions as well).";
                }

                if (addedSlider.EdgeAmount == removedSlider.EdgeAmount)
                {
                    for (var i = 0; i < addedSlider.ReverseSamplesets.Count; ++i)
                        if (addedSlider.ReverseSamplesets.ElementAtOrDefault(i) != removedSlider.ReverseSamplesets.ElementAtOrDefault(i))
                            yield return "Reverse #" + (i + 1) + " sampleset changed from " + removedSlider.ReverseSamplesets.ElementAtOrDefault(i).ToString().ToLower() + " to " + addedSlider.ReverseSamplesets.ElementAtOrDefault(i).ToString().ToLower() + ".";

                    for (var i = 0; i < addedSlider.ReverseAdditions.Count; ++i)
                        if (addedSlider.ReverseAdditions.ElementAtOrDefault(i) != removedSlider.ReverseAdditions.ElementAtOrDefault(i))
                            yield return "Reverse #" + (i + 1) + " addition changed from " + removedSlider.ReverseAdditions.ElementAtOrDefault(i).ToString().ToLower() + " to " + addedSlider.ReverseAdditions.ElementAtOrDefault(i).ToString().ToLower() + ".";

                    for (var i = 0; i < addedSlider.ReverseAdditions.Count; ++i)
                        if (addedSlider.ReverseHitSounds.ElementAtOrDefault(i) != removedSlider.ReverseHitSounds.ElementAtOrDefault(i))
                            foreach (HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds)))
                            {
                                if (addedSlider.ReverseHitSounds.ElementAtOrDefault(i).HasFlag(hitSound) && !removedSlider.ReverseHitSounds.ElementAtOrDefault(i).HasFlag(hitSound))
                                    yield return "Added " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " to reverse #" + (i + 1) + ".";

                                if (!addedSlider.ReverseHitSounds.ElementAtOrDefault(i).HasFlag(hitSound) && removedSlider.ReverseHitSounds.ElementAtOrDefault(i).HasFlag(hitSound))
                                    yield return "Removed " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " from reverse #" + (i + 1) + ".";
                            }
                }

                if (addedSlider.StartHitSound != removedSlider.StartHitSound)
                    foreach (HitObject.HitSounds hitSound in Enum.GetValues(typeof(HitObject.HitSounds)))
                    {
                        if (addedSlider.StartHitSound.HasFlag(hitSound) && !removedSlider.StartHitSound.HasFlag(hitSound))
                            yield return "Added " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " to head.";

                        if (!addedSlider.StartHitSound.HasFlag(hitSound) && removedSlider.StartHitSound.HasFlag(hitSound))
                            yield return "Removed " + Enum.GetName(typeof(HitObject.HitSounds), hitSound)?.ToLower() + " from head.";
                    }

                if (addedSlider.StartSampleset != removedSlider.StartSampleset)
                    yield return "Head sampleset changed from " + removedSlider.StartSampleset.ToString().ToLower() + " to " + addedSlider.StartSampleset.ToString().ToLower() + ".";

                if (addedSlider.StartAddition != removedSlider.StartAddition)
                    yield return "Head addition changed from " + removedSlider.StartAddition.ToString().ToLower() + " to " + addedSlider.StartAddition.ToString().ToLower() + ".";
            }

            if (type == "Spinner")
            {
                var addedSpinner = new Spinner(addedObject.code.Split(','), null!);
                var removedSpinner = new Spinner(removedObject.code.Split(','), null!);

                if (!addedSpinner.endTime.AlmostEqual(removedSpinner.endTime))
                    yield return "End time changed from " + removedSpinner.endTime + " to " + addedSpinner.endTime + ".";
            }

            if (type == "Hold note")
            {
                var addedNote = new HoldNote(addedObject.code.Split(','), null!);
                var removedNote = new HoldNote(removedObject.code.Split(','), null!);

                if (!addedNote.endTime.AlmostEqual(removedNote.endTime))
                    yield return "End time changed from " + removedNote.endTime + " to " + addedNote.endTime + ".";
            }
        }
    }
}