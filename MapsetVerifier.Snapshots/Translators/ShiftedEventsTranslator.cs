using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Snapshots.Objects;
using static MapsetVerifier.Snapshots.Snapshotter;
using static MapsetVerifier.Snapshots.Translators.SnapshotTranslation;

namespace MapsetVerifier.Snapshots.Translators;

public class ShiftedEventsTranslator : EventsTranslator
{
    public override IEnumerable<DiffInstance> Translate(
        IEnumerable<DiffInstance> diffs,
        Beatmap beatmap
    )
    {
        var changes = diffs.ToList();
        if (changes.Count == 0)
            yield break;

        var (oldCode, newCode) = LoadSnapshotCodes(beatmap, changes[0].SnapshotCreationDate);
        if (!string.IsNullOrEmpty(oldCode))
        {
            var shift = HitObjectsTranslator
                .GetCachedShiftSections(beatmap, oldCode, newCode)
                .globalShiftHint;
            var summary = GroupShiftedBreaks(changes, shift);
            if (summary != null)
                yield return summary;
        }

        foreach (var change in base.Translate(changes, beatmap))
            yield return change;
    }

    internal static DiffInstance? GroupShiftedBreaks(List<DiffInstance> changes, double shift)
    {
        if (Math.Abs(shift) < 1)
            return null;

        var breaks = changes
            .Where(diff => diff.Diff.StartsWith("2,"))
            .Select(diff => (diff, value: new Break(diff.Diff.Split(','))))
            .ToList();
        var removed = breaks.Where(item => item.diff.DiffType == DiffType.Removed).ToList();
        var consumed = new HashSet<DiffInstance>();
        var details = new List<string>();

        foreach (var added in breaks.Where(item => item.diff.DiffType == DiffType.Added))
        {
            var index = removed.FindIndex(old =>
                Math.Abs(added.value.time - old.value.time - shift) < 0.001
                && Math.Abs(added.value.endTime - old.value.endTime - shift) < 0.001
            );
            if (index < 0)
                continue;

            var old = removed[index];
            removed.RemoveAt(index);
            consumed.Add(old.diff);
            consumed.Add(added.diff);
            details.Add(
                $"{Stamp(old.value.time)} to {Stamp(old.value.endTime)} → {Stamp(added.value.time)} to {Stamp(added.value.endTime)}."
            );
        }

        if (details.Count == 0)
            return null;

        var date = changes[0].SnapshotCreationDate;
        changes.RemoveAll(consumed.Contains);
        return new DiffInstance(
            $"{details.Count} break{(details.Count == 1 ? "" : "s")} shifted in time by {shift:+0.##;-0.##;0} ms.",
            "Timing",
            DiffType.Changed,
            details,
            date
        );
    }

    private static string Stamp(double time) => Timestamp.Get(time).TrimEnd(' ', '-');
}
