using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using MathNet.Numerics;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators
{
    public class EventsTranslator : DiffTranslator
    {
        /*  First argument
            0 : Background
            1 : Video
            2 : Break
            3 : (background color transformation)
            4 : Sprite
            5 : Sample
            6 : Animation
        */

        private static List<KeyValuePair<int, DiffInstance>> _mDictionary = null!;
        public override string Section => "Events";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            _mDictionary = new List<KeyValuePair<int, DiffInstance>>();

            // Assumes all events begin with an id of their type, see block comment above.
            foreach (var diff in diffs)
                _mDictionary.Add(new KeyValuePair<int, DiffInstance>(diff.Diff[0] - 48, diff));

            // Handles all events with id 2 (i.e. breaks).
            foreach (var diff in GetBreakTranslation())
                yield return diff;

            // Handles all other events.
            foreach (var diff in _mDictionary.Select(pair => pair.Value))
                yield return diff;
        }

        private IEnumerable<DiffInstance> GetBreakTranslation()
        {
            var addedBreaks = _mDictionary.Where(pair => pair.Key == 2 && pair.Value.DiffType == DiffType.Added).Select(pair => new Tuple<Break, DiffInstance>(new Break(pair.Value.Diff.Split(',')), pair.Value)).ToList();

            var removedBreaks = _mDictionary.Where(pair => pair.Key == 2 && pair.Value.DiffType == DiffType.Removed).Select(pair => new Tuple<Break, DiffInstance>(new Break(pair.Value.Diff.Split(',')), pair.Value)).ToList();

            _mDictionary.RemoveAll(pair => pair.Key == 2);

            foreach (var (@break, diffInstance) in addedBreaks)
            {
                var removedStart = removedBreaks.FirstOrDefault(tuple => tuple.Item1.time.AlmostEqual(@break.time));

                var startStamp = Timestamp.Get(@break.time);
                var endStamp = Timestamp.Get(@break.endTime);

                if (removedStart != null)
                {
                    var oldStartStamp = Timestamp.Get(removedStart.Item1.time);
                    var oldEndStamp = Timestamp.Get(removedStart.Item1.endTime);

                    yield return new DiffInstance("Break from " + oldStartStamp + " to " + oldEndStamp + " now ends at " + endStamp + " instead.", Section, DiffType.Changed, new List<string>(), diffInstance.SnapshotCreationDate);

                    removedBreaks.Remove(removedStart);
                }
                else
                {
                    var removedEnd = removedBreaks.FirstOrDefault(tuple => tuple.Item1.endTime.AlmostEqual(@break.endTime));

                    if (removedEnd != null)
                    {
                        var oldStartStamp = Timestamp.Get(removedEnd.Item1.time);
                        var oldEndStamp = Timestamp.Get(removedEnd.Item1.endTime);

                        yield return new DiffInstance("Break from " + oldStartStamp + " to " + oldEndStamp + " now starts at " + startStamp + " instead.", Section, DiffType.Changed, new List<string>(), diffInstance.SnapshotCreationDate);

                        removedBreaks.Remove(removedEnd);
                    }
                    else
                    {
                        yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " added.", Section, DiffType.Added, new List<string>(), diffInstance.SnapshotCreationDate);
                    }
                }
            }

            foreach (var (@break, diffInstance) in removedBreaks)
            {
                var startStamp = Timestamp.Get(@break.time);
                var endStamp = Timestamp.Get(@break.endTime);

                yield return new DiffInstance("Break from " + startStamp + " to " + endStamp + " removed.", Section, DiffType.Removed, new List<string>(), diffInstance.SnapshotCreationDate);
            }
        }
    }
}