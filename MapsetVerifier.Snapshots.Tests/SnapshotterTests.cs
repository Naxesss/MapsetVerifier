using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary>
/// Regression tests for the bugs fixed in <see cref="Snapshotter" />:
/// - an early `return` in the duplicate-beatmap check used to abort the whole snapshot pass,
///   silently skipping the "files"/General snapshot for every beatmap after it.
/// - `Nullable&lt;ulong&gt;.ToString()` returns "" (not null) for unsubmitted difficulties, so the
///   null-guard never fired, unsubmitted difficulties all collapsed onto the same save folder
///   (Path.Combine drops empty segments), and `null == null` made unrelated unsubmitted
///   difficulties look like "duplicates" of each other, permanently skipping all but one.
/// - the duplicate-beatmap date check resolved `Beatmap.MapPath` (relative to the song folder)
///   as if it were directly usable by <see cref="File.GetCreationTimeUtc" />, so it always
///   compared nonexistent paths instead of the real files.
/// - `GetSnapshots` only matched "*.osu" files, but the "files"/General history is saved as
///   ".txt", so General history could never be read back even when it was saved correctly.
/// </summary>
public class SnapshotterTests
{
    private static string BuildOsu(
        string version,
        string hitObjectTime,
        ulong? beatmapId = null,
        ulong? beatmapSetId = 12345
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:" + version,
            "BeatmapID:" + (beatmapId?.ToString() ?? "0"),
            "BeatmapSetID:" + (beatmapSetId?.ToString() ?? "-1"),
            "[Difficulty]",
            "StackLeniency:0.7",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            hitObjectTime + ",192,1000,1,0,0:0:0:0:",
        };

        return string.Join("\n", lines);
    }

    [Fact]
    public void DuplicateOlderBeatmap_DoesNotBlockGeneralFilesSnapshot()
    {
        using var context = SnapshotterTestContext.Create();

        // Two files that resolve to the same real (submitted) beatmap id - the kind of
        // true duplicate the check was designed for - plus an unrelated third difficulty.
        context.WriteDifficulty(
            "old.osu",
            BuildOsu("Old Copy", "100", beatmapId: 111),
            DateTime.UtcNow.AddDays(-1)
        );
        context.WriteDifficulty(
            "new.osu",
            BuildOsu("New Copy", "100", beatmapId: 111),
            DateTime.UtcNow
        );
        context.WriteDifficulty("other.osu", BuildOsu("Other", "150", beatmapId: 222));
        context.WriteExtraFile("audio.mp3");

        var beatmapSet = context.LoadBeatmapSet();
        Snapshotter.SnapshotBeatmapSet(beatmapSet);

        // Before the fix, hitting the older duplicate returned out of SnapshotBeatmapSet
        // entirely, so SnapshotFiles (which produces the "General" history) never ran.
        var generalSnapshots = Snapshotter.GetSnapshots("12345", "files").ToList();
        Assert.Single(generalSnapshots);

        // The unrelated difficulty must still get its own snapshot saved too.
        var otherBeatmap = beatmapSet.Beatmaps.Single(b => b.MetadataSettings.version == "Other");
        Assert.Single(Snapshotter.GetSnapshots(otherBeatmap));
    }

    [Fact]
    public void GeneralFilesSnapshots_CanBeRetrievedAndDiffedAcrossMultipleSaves()
    {
        using var context = SnapshotterTestContext.Create();

        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "100"));
        context.WriteExtraFile("audio.mp3");

        Snapshotter.SnapshotBeatmapSet(context.LoadBeatmapSet());

        // Snapshot file names only have second resolution, so give the next save a
        // distinct timestamp to land in.
        Thread.Sleep(1100);

        // Add a new file so the "files" hash listing actually changes.
        context.WriteExtraFile("hitsound.wav");
        Snapshotter.SnapshotBeatmapSet(context.LoadBeatmapSet());

        // Saved as ".txt", not ".osu" - GetSnapshots must still find both of them.
        var generalSnapshots = Snapshotter
            .GetSnapshots("12345", "files")
            .OrderBy(s => s.creationTime)
            .ToList();
        Assert.Equal(2, generalSnapshots.Count);

        var diffs = Snapshotter.Compare(generalSnapshots[0], generalSnapshots[1].code).ToList();
        Assert.Contains(diffs, diff => diff.Diff.Contains("hitsound.wav"));
    }

    [Fact]
    public void MultipleUnsubmittedDifficulties_AreTrackedSeparately()
    {
        using var context = SnapshotterTestContext.Create();

        // Neither difficulty has a real BeatmapID (both are unsubmitted, i.e. beatmapId is null
        // once parsed). Before the fix these both resolved to the same save folder.
        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "100"));
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "100"));
        context.WriteExtraFile("audio.mp3");

        var beatmapSet = context.LoadBeatmapSet();
        Snapshotter.SnapshotBeatmapSet(beatmapSet);

        var easy = beatmapSet.Beatmaps.Single(b => b.MetadataSettings.version == "Easy");
        var hard = beatmapSet.Beatmaps.Single(b => b.MetadataSettings.version == "Hard");

        var easySnapshots = Snapshotter.GetSnapshots(easy).ToList();
        var hardSnapshots = Snapshotter.GetSnapshots(hard).ToList();

        // Both difficulties must have their own recorded snapshot ...
        Assert.Single(easySnapshots);
        Assert.Single(hardSnapshots);

        // ... containing that difficulty's own code, not a mix/overwrite of the other's.
        Assert.Contains("Version:Easy", easySnapshots[0].code);
        Assert.Contains("Version:Hard", hardSnapshots[0].code);
    }

    [Fact]
    public void EditingOneUnsubmittedDifficulty_DoesNotAffectSiblingsHistory()
    {
        using var context = SnapshotterTestContext.Create();

        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "100"));
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "100"));
        context.WriteExtraFile("audio.mp3");

        Snapshotter.SnapshotBeatmapSet(context.LoadBeatmapSet());

        // Snapshot file names only have second resolution, so give the next save a
        // distinct timestamp to land in - otherwise it'd overwrite the first snapshot file.
        Thread.Sleep(1100);

        // Only "Hard" changes on the next save.
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "200"));

        var beatmapSet = context.LoadBeatmapSet();
        Snapshotter.SnapshotBeatmapSet(beatmapSet);

        var easy = beatmapSet.Beatmaps.Single(b => b.MetadataSettings.version == "Easy");
        var hard = beatmapSet.Beatmaps.Single(b => b.MetadataSettings.version == "Hard");

        Assert.Single(Snapshotter.GetSnapshots(easy));
        Assert.Equal(2, Snapshotter.GetSnapshots(hard).Count());
    }
}
