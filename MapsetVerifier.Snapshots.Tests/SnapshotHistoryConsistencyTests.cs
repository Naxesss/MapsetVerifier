using MapsetVerifier.Server.Service;
using MapsetVerifier.Snapshots;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary>
/// Regression tests for <see cref="SnapshotService.GetSnapshots" />: every difficulty (and
/// General) should share the exact same set of selectable snapshot dates once they have any
/// history at all, even for dates where that particular difficulty/General had no changes.
/// Previously General only listed dates where its own files-hash listing changed, and both
/// General and per-difficulty histories dropped a date entirely when there was nothing to diff
/// yet, so switching tabs in the Electron snapshot screen showed a different set of selectable
/// commits per difficulty.
/// </summary>
public class SnapshotHistoryConsistencyTests
{
    private static string BuildOsu(string version, string hitObjectTime) =>
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
            hitObjectTime + ",192,1000,1,0,0:0:0:0:"
        );

    [Fact]
    public void AllHistories_ShareTheSameCommitDates_EvenWhenOnlyOneDifficultyChanged()
    {
        using var context = SnapshotterTestContext.Create();

        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "100"));
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "100"));
        context.WriteExtraFile("audio.mp3");
        SnapshotService.GetSnapshots(context.SongPath);

        // Only "Hard" changes; "Easy" and the files listing (General) stay untouched.
        Thread.Sleep(1100);
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "200"));
        SnapshotService.GetSnapshots(context.SongPath);

        // Only "Easy" changes this time.
        Thread.Sleep(1100);
        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "200"));
        var result = SnapshotService.GetSnapshots(context.SongPath);

        var easyHistory = result.BeatmapHistories.Single(h => h.DifficultyName == "Easy");
        var hardHistory = result.BeatmapHistories.Single(h => h.DifficultyName == "Hard");
        var generalHistory = result.General!.Value;

        var easyDates = easyHistory.Commits.Select(c => c.Id).ToHashSet();
        var hardDates = hardHistory.Commits.Select(c => c.Id).ToHashSet();
        var generalDates = generalHistory.Commits.Select(c => c.Id).ToHashSet();

        // Every difficulty, and General, must offer the exact same set of selectable commit
        // dates (one per save-with-an-edit-somewhere) - even on the dates where that specific
        // difficulty/General itself had no changes.
        Assert.Equal(2, easyDates.Count);
        Assert.Equal(easyDates, hardDates);
        Assert.Equal(easyDates, generalDates);

        var easyCommits = easyHistory.Commits.OrderBy(c => c.Date).ToList();
        var hardCommits = hardHistory.Commits.OrderBy(c => c.Date).ToList();
        var generalCommits = generalHistory.Commits.OrderBy(c => c.Date).ToList();

        // First save-with-a-change: only "Hard" actually changed. "Easy" still gets a commit
        // for this date (that's the whole point), it just genuinely reports no changes.
        // General also legitimately shows a change here: it hashes every file including
        // .osu's own contents, so Hard.osu's hash changing is a real General-level change too.
        Assert.Equal(0, easyCommits[0].TotalChanges);
        Assert.True(hardCommits[0].TotalChanges > 0);
        Assert.True(generalCommits[0].TotalChanges > 0);

        // Second save-with-a-change: only "Easy" actually changed.
        Assert.True(easyCommits[1].TotalChanges > 0);
        Assert.Equal(0, hardCommits[1].TotalChanges);
        Assert.True(generalCommits[1].TotalChanges > 0);

        // Both difficulties existed since the very first save, so every commit is a real
        // snapshot - never a "no snapshot yet" placeholder.
        Assert.All(easyCommits, c => Assert.True(c.HasSnapshot));
        Assert.All(hardCommits, c => Assert.True(c.HasSnapshot));
    }

    [Fact]
    public void DifficultyAddedLater_GetsNoSnapshotPlaceholder_ForEarlierDates()
    {
        using var context = SnapshotterTestContext.Create();

        // Only "Easy" exists at first.
        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "100"));
        context.WriteExtraFile("audio.mp3");
        SnapshotService.GetSnapshots(context.SongPath);

        // "Easy" is edited, "Hard" still doesn't exist yet.
        Thread.Sleep(1100);
        context.WriteDifficulty("diffA.osu", BuildOsu("Easy", "200"));
        SnapshotService.GetSnapshots(context.SongPath);

        // "Hard" is added for the first time.
        Thread.Sleep(1100);
        context.WriteDifficulty("diffB.osu", BuildOsu("Hard", "100"));
        var result = SnapshotService.GetSnapshots(context.SongPath);

        var easyHistory = result.BeatmapHistories.Single(h => h.DifficultyName == "Easy");
        var hardHistory = result.BeatmapHistories.Single(h => h.DifficultyName == "Hard");
        var generalHistory = result.General!.Value;

        // "Hard" must still expose the same two selectable dates as "Easy" and General -
        // this is the exact scenario reported: a difficulty whose own oldest snapshot is much
        // newer than the mapset's overall oldest snapshot didn't show up consistently.
        var easyDates = easyHistory.Commits.Select(c => c.Id).ToHashSet();
        var hardDates = hardHistory.Commits.Select(c => c.Id).ToHashSet();
        var generalDates = generalHistory.Commits.Select(c => c.Id).ToHashSet();

        Assert.Equal(2, easyDates.Count);
        Assert.Equal(easyDates, hardDates);
        Assert.Equal(easyDates, generalDates);

        var hardCommits = hardHistory.Commits.OrderBy(c => c.Date).ToList();

        // At the first date, "Hard" didn't exist yet - a placeholder, not a real snapshot.
        Assert.False(hardCommits[0].HasSnapshot);
        Assert.Equal(0, hardCommits[0].TotalChanges);

        // At the second date, "Hard" was introduced - a real (if zero-diff) snapshot.
        Assert.True(hardCommits[1].HasSnapshot);
    }
}
