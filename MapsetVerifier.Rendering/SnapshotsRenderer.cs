using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Rendering
{
    public abstract class SnapshotsRenderer : BeatmapInfoRenderer
    {
        private static List<DateTime> _snapshotDates = [];

        public static string Render(BeatmapSet beatmapSet)
        {
            InitSnapshotDates(beatmapSet);

            return string.Concat(
                RenderBeatmapInfo(beatmapSet),
                RenderSnapshotInterpretation(),
                RenderSnapshotDifficulties(beatmapSet),
                RenderBeatmapSnapshots(beatmapSet));
        }

        private static string RenderSnapshotInterpretation() =>
            Div("",
                DivAttr("interpret-container", DataAttr("interpret", "difficulty"),
                    _snapshotDates.Select((date, index) =>
                        DivAttr("interpret" + (index == _snapshotDates.Count - 2
                            ? " interpret-selected"
                            : index == _snapshotDates.Count - 1
                                ? " interpret-default" + (_snapshotDates.Count == 1
                                    ? " interpret-selected"
                                    : "")
                                : ""), DataAttr("interpret-severity", index),
                            date.ToString("yyyy-MM-dd HH:mm:ss"))).ToArray<object?>()));

        private static string RenderSnapshotDifficulties(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];
            var defaultIcon = "gear-gray";

            return
                Div("beatmap-difficulties",
                    DivAttr("beatmap-difficulty noselect", DataAttr("difficulty", "Files"),
                        Div("medium-icon " + defaultIcon + "-icon"),
                        Div("difficulty-name",
                            "General")) +
                    string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
                    {
                        var version = Encode(beatmap.MetadataSettings.version);

                        return
                            DivAttr("beatmap-difficulty noselect" + (beatmap == refBeatmap ? " beatmap-difficulty-selected" : ""), DataAttr("difficulty", version),
                                Div("medium-icon " + defaultIcon + "-icon"),
                                Div("difficulty-name",
                                    version));
                    })));
        }

        private static string RenderBeatmapSnapshots(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            if (refBeatmap.MetadataSettings.beatmapSetId == null)
                return Div("paste-separator") + Div("unsubmitted-snapshot-error", "Beatmapset ID is -1. This makes the map ambigious with any other " + "unsubmitted map. Submit the map before using this feature.");

            // Just need the beatmapset id to know in which snapshot folder to look for the files.
            var beatmapSetId = refBeatmap.MetadataSettings.beatmapSetId.ToString();
            
            // Can't do snapshots without a beatmap set id.
            if (beatmapSetId == null)
            {
                return "";
            }
            
            var refSnapshots = Snapshotter.GetSnapshots(beatmapSetId, "files").ToArray();

            var lastSnapshot = refSnapshots.First(snapshot => snapshot.creationTime == refSnapshots.Max(otherSnapshot => otherSnapshot.creationTime));

            var refDiffs = new List<DiffInstance>();

            foreach (var refSnapshot in refSnapshots)
            {
                IEnumerable<DiffInstance> refDiffsCompare = Snapshotter.Compare(refSnapshot, lastSnapshot.code).ToList();

                refDiffs.AddRange(Snapshotter.TranslateComparison(refDiffsCompare));
            }

            return Div("paste-separator") + Div("card-container-unselected", DivAttr("card-difficulty", DataAttr("difficulty", "Files"), RenderSnapshotSections(refDiffs, refSnapshots, "Files", true)) + string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
            {
                // Comparing current to all previous snapshots for this beatmap so the user
                // can pick interpretation without loading in-between.
                IEnumerable<Snapshotter.Snapshot> snapshots = Snapshotter.GetSnapshots(beatmap).ToList();
                var diffs = new List<DiffInstance>();

                foreach (var snapshot in snapshots)
                {
                    IEnumerable<DiffInstance> diffsCompare = Snapshotter.Compare(snapshot, beatmap.Code).ToList();

                    diffs.AddRange(Snapshotter.TranslateComparison(diffsCompare));
                }

                var version = Encode(beatmap.MetadataSettings.version);

                return DivAttr("card-difficulty", DataAttr("difficulty", version), RenderSnapshotSections(diffs, snapshots, version));
            }))) + Div("paste-separator select-separator") + Div("card-container-selected");
        }

        private static string RenderSnapshotSections(IEnumerable<DiffInstance> beatmapDiffs, IEnumerable<Snapshotter.Snapshot> snapshots, string? version, bool files = false) =>
            Div("card-difficulty-checks", beatmapDiffs.Where(diff => files ? diff.Section == "Files" : diff.Section != "Files").GroupBy(diff => diff.Section).Select(sectionDiffs => DivAttr("card", DataAttr("difficulty", version), Div("card-box shadow noselect", Div("large-icon " + GetIcon(sectionDiffs) + "-icon"), Div("card-title", Encode(sectionDiffs.Key))), Div("card-details-container", Div("card-details", RenderSnapshotDiffs(sectionDiffs, snapshots))))).ToArray());

        private static string RenderSnapshotDiffs(IEnumerable<DiffInstance> sectionDiffs, IEnumerable<Snapshotter.Snapshot> snapshots) =>
            string.Concat(sectionDiffs.Select(diff =>
            {
                var message = FormatTimestamps(Encode(diff.Diff)!);
                var condition = GetDiffCondition(diff, snapshots);

                return DivAttr("card-detail", DataAttr("condition", "difficulty=" + condition), Div("card-detail-icon " + GetIcon(diff) + "-icon"), diff.Details.Any() ? Div("", Div("card-detail-text", message), Div("vertical-arrow card-detail-toggle")) : Div("card-detail-text", message)) + RenderDiffDetails(diff.Details, condition);
            }));

        private static string RenderDiffDetails(List<string> details, string condition)
        {
            var detailIcon = "gear-blue";

            return Div("card-detail-instances", details.Select(detail =>
            {
                var timestampedMessage = FormatTimestamps(Encode(detail)!);

                if (timestampedMessage.Length == 0)
                    return "";

                return DivAttr("card-detail", DataAttr("condition", "difficulty=" + condition), Div("card-detail-icon " + detailIcon + "-icon"), Div("", timestampedMessage));
            }).ToArray());
        }

        private static string GetDiffCondition(DiffInstance diff, IEnumerable<Snapshotter.Snapshot> snapshots)
        {
            snapshots = snapshots.ToArray();

            var myNextIndex = snapshots.ToList().FindLastIndex(snapshot => snapshot.creationTime == diff.SnapshotCreationDate) + 1;

            var myNextDate = myNextIndex >= snapshots.Count() ? snapshots.Last().creationTime : snapshots.ElementAt(myNextIndex).creationTime;

            var indexes = new List<int>();

            for (var i = 0; i < _snapshotDates.Count; ++i)
                if (_snapshotDates.ElementAt(i) < myNextDate && _snapshotDates.ElementAt(i) >= diff.SnapshotCreationDate)
                    indexes.Add(i);

            return string.Join(",", indexes);
        }

        private static void InitSnapshotDates(BeatmapSet beatmapSet)
        {
            _snapshotDates = beatmapSet.Beatmaps.SelectMany(beatmap => Snapshotter.GetSnapshots(beatmap).Select(snapshot => snapshot.creationTime)).ToList();

            _snapshotDates.AddRange(Snapshotter.GetSnapshots(beatmapSet.Beatmaps.First().MetadataSettings.beatmapSetId.ToString(), "files").Select(snapshot => snapshot.creationTime));

            _snapshotDates = _snapshotDates.Distinct().OrderBy(date => date).ToList();
        }

        private static string GetIcon(DiffInstance diff) =>
            diff.DiffType == Snapshotter.DiffType.Changed ? "gear-blue" :
            diff.DiffType == Snapshotter.DiffType.Added ? "plus" :
            diff.DiffType == Snapshotter.DiffType.Removed ? "minus" : "gear-gray";

        private static string GetIcon(IEnumerable<DiffInstance> diffs)
        {
            diffs = diffs.ToArray();

            return (diffs.Any(diff => diff.DiffType == Snapshotter.DiffType.Added) && diffs.Any(diff => diff.DiffType == Snapshotter.DiffType.Removed)) || diffs.Any(diff => diff.DiffType == Snapshotter.DiffType.Changed) ? "gear-blue" :
                diffs.Any(diff => diff.DiffType == Snapshotter.DiffType.Added) ? "plus" :
                diffs.Any(diff => diff.DiffType == Snapshotter.DiffType.Removed) ? "minus" : "gear-gray";
        }
    }
}