using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary>
/// Confirms Snapshotter's existing "shouldSave" dedup (comparing the latest saved snapshot's
/// content against the current beatmap code / files-hash listing) actually holds up when a
/// mapset is reparsed with no real edits - which used to look broken before the folder-collision
/// and path-resolution bugs were fixed, since those made unrelated snapshots overwrite each other
/// and appear to "change" on every reparse even though nothing really did.
/// </summary>
public class SnapshotDeduplicationTests
{
    private static string BuildOsu(string version) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:" + version,
            "BeatmapID:0",
            "BeatmapSetID:12345",
            "[Difficulty]",
            "StackLeniency:0.7",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            "100,192,1000,1,0,0:0:0:0:"
        );

    [Fact]
    public void ReparsingUnchangedMapset_DoesNotCreateNewSnapshots()
    {
        using var context = SnapshotterTestContext.Create();

        context.WriteDifficulty("diffA.osu", BuildOsu("Easy"));
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard"));
        context.WriteExtraFile("audio.mp3");
        for (var i = 0; i < 20; i++)
            context.WriteExtraFile($"sound{i}.wav", i.ToString());

        Snapshotter.SnapshotBeatmapSet(context.LoadBeatmapSet());

        for (var i = 0; i < 5; i++)
        {
            // Force a distinct wall-clock second so a real (bugged) duplicate save would be
            // visibly distinguishable from the first one rather than silently overwriting it.
            Thread.Sleep(1100);

            var beatmapSet = context.LoadBeatmapSet();
            Snapshotter.SnapshotBeatmapSet(beatmapSet);

            foreach (var beatmap in beatmapSet.Beatmaps)
                Assert.Single(Snapshotter.GetSnapshots(beatmap));

            Assert.Single(Snapshotter.GetSnapshots("12345", "files"));
        }
    }
}
