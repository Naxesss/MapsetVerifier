using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using Serilog;
using static MapsetVerifier.Snapshots.Snapshotter;

namespace MapsetVerifier.Snapshots.Translators;

internal static class SnapshotTranslation
{
    public static (string? oldCode, string? newCode) LoadSnapshotCodes(
        Beatmap beatmap,
        DateTime creationDate
    )
    {
        var setId = beatmap.MetadataSettings.beatmapSetId?.ToString();
        var mapId = beatmap.MetadataSettings.beatmapId?.ToString();
        if (setId == null || mapId == null)
            return (null, null);

        try
        {
            var snapshots = GetSnapshots(setId, mapId)
                .OrderBy(snapshot => snapshot.creationTime)
                .ToList();
            var index = snapshots.FindIndex(snapshot => snapshot.creationTime == creationDate);
            return index < 0
                ? (null, null)
                : (
                    snapshots[index].code,
                    index + 1 < snapshots.Count ? snapshots[index + 1].code : null
                );
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Could not load snapshot for alignment");
            return (null, null);
        }
    }

    public static double GetSnapTolerance(Beatmap beatmap, double time) =>
        beatmap.GetTimingLine<UninheritedLine>(time)?.msPerBeat / 32 ?? 2;

    public static (string prefix, string suffix) BuildRemovedStamp(double oldTime, double shift) =>
        Math.Abs(shift) < 1
            ? (Timestamp.Get(oldTime), "")
            : (
                Timestamp.Get(oldTime + shift),
                $" (originally {Timestamp.Get(oldTime).TrimEnd(' ', '-')})"
            );
}
