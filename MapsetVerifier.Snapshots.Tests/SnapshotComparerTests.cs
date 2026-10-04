using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotComparerTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (SnapshotEntry Before, SnapshotEntry After) Capture(
        StoreTestContext context,
        Action change
    )
    {
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        change();
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(5))!;

        return (before, after);
    }

    [Fact]
    public void MovedObject_ShowsAsOneHunkWithAVisual()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(6), beatmapId: 1));

        var (before, after) = Capture(
            context,
            () =>
            {
                var objects = Circles(6);
                objects[2] = "300,200,2000,1,0,0:0:0:0:";
                context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
            }
        );

        var result = SnapshotComparer.Compare("set-12345", before, after);
        var easy = Assert.Single(result.Difficulties);
        var hunk = Assert.Single(easy.Hunks);

        Assert.Equal(DifficultyStatus.Changed, easy.Status);
        Assert.Equal(1, easy.Counts.Changed);
        Assert.Equal(ChangeKind.Placement, Assert.Single(hunk.Kinds));
        Assert.Contains(easy.Marks, m => m.Kind == ChangeKind.Placement);
    }

    [Fact]
    public void AMoveAndAHitsoundOnTheSameObject_AreSeparateHunks_AndOnlyTheMoveIsDrawn()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(6), beatmapId: 1));

        var (before, after) = Capture(
            context,
            () =>
            {
                var objects = Circles(6);
                objects[2] = "300,200,2000,1,2,0:0:0:0:"; // moved, and a whistle added
                context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
            }
        );

        var easy = Assert.Single(SnapshotComparer.Compare("set-12345", before, after).Difficulties);

        Assert.Equal(2, easy.Hunks.Count);
        var move = Assert.Single(easy.Hunks, h => h.Kinds.Contains(ChangeKind.Placement));
        var sound = Assert.Single(easy.Hunks, h => h.Kinds.Contains(ChangeKind.Hitsound));
        Assert.Equal(HunkLabel.Hitsounding, sound.Label);
        Assert.Equal(1, move.Counts.Changed);
        Assert.Equal(1, sound.Counts.Changed);
    }

    [Fact]
    public void AWindow_IsAlwaysTheSameNumberOfBeats_AndScrollsHalfABeatAtATime()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(40), beatmapId: 1));

        var (before, after) = Capture(
            context,
            () =>
            {
                var objects = Circles(40);
                objects[20] = "300,200,11000,1,0,0:0:0:0:";
                context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
            }
        );

        // 500 ms beats: a window is 4 beats, 2000 ms. A one-object change at 11000 ms is centred.
        var window = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 11000, 11000, 0);

        Assert.Equal(2000, window.To - window.From);
        Assert.Equal(10000, window.From);
        Assert.Equal(12000, window.To);
        Assert.Equal(4, window.Objects); // 10000, 10500, 11000, 11500
        // Each side reaches a beat past both ends, so objects at the edges can find their pair.
        Assert.Equal(6, window.Visual.After.Count);
        Assert.Equal(9500, window.Visual.After[0].Time);
        Assert.Contains(window.Visual.After, o => o.X == 300);
        Assert.True(window.HasEarlier);
        Assert.True(window.HasLater);
        Assert.Equal(500, Assert.Single(window.Visual.AfterTiming).BeatLength);

        // A longer change starts one beat in, and is scrolled through. The size never changes.
        var wide = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 5000, 15000, 0);
        Assert.Equal(2000, wide.To - wide.From);
        Assert.Equal(4500, wide.From);

        var one = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 5000, 15000, 1);
        var back = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 5000, 15000, -1);
        Assert.Equal(4750, one.From);
        Assert.Equal(4250, back.From);
        Assert.Equal(2000, one.To - one.From);

        // The ends of the song: nothing more to scroll to, and the size stays.
        var end = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 5000, 15000, 500);
        Assert.False(end.HasLater);
        Assert.Equal(2000, end.To - end.From);
        var start = SnapshotComparer.GetWindow(
            "set-12345",
            before,
            after,
            "b-1",
            5000,
            15000,
            -500
        );
        Assert.False(start.HasEarlier);
    }

    [Fact]
    public void ASettingChangedInEveryDifficulty_IsShownOnceInGeneral()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(4), beatmapId: 2));

        var (before, after) = Capture(
            context,
            () =>
            {
                context.WriteDifficulty(
                    "a.osu",
                    BuildOsu("Easy", Circles(3), beatmapId: 1).Replace("alpha beta", "alpha gamma")
                );
                context.WriteDifficulty(
                    "b.osu",
                    BuildOsu("Hard", Circles(4), beatmapId: 2).Replace("alpha beta", "alpha gamma")
                );
            }
        );

        var result = SnapshotComparer.Compare("set-12345", before, after);
        var tags = Assert.Single(result.General.Settings);

        Assert.Equal("Tags", tags.Key);
        Assert.Equal(2, tags.AppliesTo);
        Assert.Equal(["gamma"], tags.Added);
        Assert.Equal(["beta"], tags.Removed);
        Assert.All(result.Difficulties, d => Assert.Empty(d.Settings));
    }

    [Fact]
    public void ASettingChangedInOneDifficulty_StaysThere()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(4), beatmapId: 2));

        var (before, after) = Capture(
            context,
            () =>
                context.WriteDifficulty(
                    "b.osu",
                    BuildOsu("Hard", Circles(4), beatmapId: 2, extra: "ApproachRate:9")
                )
        );

        var result = SnapshotComparer.Compare("set-12345", before, after);

        Assert.Empty(result.General.Settings);
        Assert.Equal(
            DifficultyStatus.Unchanged,
            result.Difficulties.Single(d => d.Name == "Easy").Status
        );
        Assert.Contains(
            result.Difficulties.Single(d => d.Name == "Hard").Settings,
            s => s.Key == "ApproachRate" && s.After == "9"
        );
    }

    [Fact]
    public void ASharedOffsetShift_IsOneGeneralRollup()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(8), beatmapId: 1));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(8, x: 80), beatmapId: 2));

        var (before, after) = Capture(
            context,
            () =>
            {
                var timing = "6,500,4,2,0,100,1,0";
                context.WriteDifficulty(
                    "a.osu",
                    BuildOsu("Easy", Circles(8, start: 1006), beatmapId: 1, timing: timing)
                );
                context.WriteDifficulty(
                    "b.osu",
                    BuildOsu("Hard", Circles(8, start: 1006, x: 80), beatmapId: 2, timing: timing)
                );
            }
        );

        var result = SnapshotComparer.Compare("set-12345", before, after);
        var rollup = Assert.Single(result.General.Rollups);

        Assert.Equal(6, rollup.Amount);
        Assert.All(result.Difficulties, d => Assert.Empty(d.Hunks));
        Assert.All(result.Difficulties, d => Assert.Empty(d.Rollups));
    }

    [Fact]
    public void ReplacedAndAddedFiles_AreListedByKind()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteFile("audio.mp3", "one");

        var (before, after) = Capture(
            context,
            () =>
            {
                context.WriteFile("audio.mp3", "two!");
                context.WriteFile("bg.jpg", "img");
            }
        );

        var files = SnapshotComparer.Compare("set-12345", before, after).General.Files;

        Assert.Contains(
            files,
            f =>
                f.Name == "audio.mp3"
                && f.Category == "Audio"
                && f.Op == ChangeOp.Changed
                && f.SizeAfter == 4
        );
        Assert.Contains(
            files,
            f => f.Name == "bg.jpg" && f.Category == "Image" && f.Op == ChangeOp.Added
        );
    }

    [Fact]
    public void History_ListsNewestFirst_WithCountsPerChangedDifficulty()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(3), beatmapId: 1));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(4), beatmapId: 2));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);

        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(5), beatmapId: 2));
        SnapshotStore.Capture(context.Load(), "checkRun", now: T0.AddHours(1));

        var history = SnapshotHistory.Build("set-12345");

        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("checkRun", history.Entries[0].Trigger);
        Assert.True(history.Entries[1].IsFirst);
        Assert.Equal(["b-2"], history.Entries[0].ChangedDifficulties);
        Assert.Equal(1, history.Entries[0].Counts.Added);
        Assert.Equal(2, history.Difficulties.Count);
        Assert.NotNull(SnapshotStore.Load("set-12345").Entries[1].Summary);
    }

    [Fact]
    public void ABackgroundWithTheSameFile_ReportsWhatActuallyChanged()
    {
        static string Events(string line) =>
            "[Events]" + Environment.NewLine + line + Environment.NewLine;

        var before = Events("0,0,\"bg.jpg\",0,0");
        var moved = SettingsDiffer.Diff(before, Events("0,0,\"bg.jpg\",20,-10"));
        var shifted = SettingsDiffer.Diff(before, Events("0,500,\"bg.jpg\",0,0"));
        var replaced = SettingsDiffer.Diff(before, Events("0,0,\"other.jpg\",0,0"));

        var position = Assert.Single(moved);
        Assert.Equal("Background position", position.Key);
        Assert.Equal("0, 0", position.Before);
        Assert.Equal("20, -10", position.After);

        Assert.Equal("Background start time", Assert.Single(shifted).Key);

        var replacement = Assert.Single(replaced);
        Assert.Equal("Background", replacement.Key);
        Assert.Equal("bg.jpg", replacement.Before);
        Assert.Equal("other.jpg", replacement.After);
    }
}
