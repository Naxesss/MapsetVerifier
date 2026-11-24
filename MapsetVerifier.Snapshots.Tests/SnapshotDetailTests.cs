using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotDetailTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static SnapshotEntry Capture(
        StoreTestContext context,
        int minute,
        string[] objects,
        ulong? beatmapId = 1,
        string timing = "0,500,4,2,0,100,1,0"
    )
    {
        context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId, timing: timing));

        return SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(minute))!;
    }

    [Fact]
    public void Length_IsUntilTheLastObjectEnds()
    {
        using var context = Create();
        var before = Capture(context, 0, Circles(3));
        var after = Capture(
            context,
            60,
            ["256,192,500,12,0,9000,0:0:0:0:", "100,100,9500,1,0,0:0:0:0:"]
        );

        Assert.Equal(
            9500,
            Assert
                .Single(SnapshotComparer.Compare("set-12345", before, after).Difficulties)
                .LengthMs
        );
    }

    [Fact]
    public void OnlyTheFirstSnapshotWithIds_IsTheSubmittedOne()
    {
        using var context = Create();
        Capture(context, 0, Circles(3), null);
        Capture(context, 60, Circles(4));
        Capture(context, 120, Circles(5));
        Capture(context, 180, Circles(6));

        Assert.Equal(
            [null, "Submitted", null, null],
            SnapshotStore.Load("set-12345").Entries.Select(entry => entry.Pin)
        );
    }

    [Fact]
    public void Sets_AreStoredUnderTheirFirstKnownId()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("A", Circles(3), setId: 0));
        context.WriteDifficulty("b.osu", BuildOsu("B", Circles(4), setId: 77));

        Assert.Equal("set-77", SnapshotStore.ResolveSetKey(context.Load()));
    }

    [Fact]
    public void SlidersWithoutReverses_HaveNoEdges()
    {
        using var context = Create();
        var entry = Capture(
            context,
            0,
            ["100,100,1000,2,0,L|240:100,1,140", "100,300,9000,1,0,0:0:0:0:"]
        );
        var window = SnapshotComparer.GetWindow("set-12345", entry, entry, "b-1", 1000, 1000, 0);

        Assert.Null(window.Visual.After.First().Edges);
    }

    private static Beatmap Build(params string[] objects) =>
        new(BuildOsu("Test", objects, beatmapId: 1), "song", $"{Guid.NewGuid():N}.osu");

    /// <summary> Objects which were also moved elsewhere are not part of what was only shifted. </summary>
    [Fact]
    public void Shift_IsOnlyOfWhatStayedInPlace()
    {
        // All far apart from each other, so only the same circle can be taken for a shifted one.
        string[] Circles(int shift, params int[] moved) =>
            Enumerable
                .Range(0, 20)
                .Select(i =>
                    $"{i * 131 % 500},{(moved.Contains(i) ? 350 : 50)},{1000 + i * 700 + shift},1,0,0:0:0:0:"
                )
                .ToArray();

        var result = BeatmapDiffer.Diff(Build(Circles(0)), Build(Circles(300, 5, 12)));
        var plain = BeatmapDiffer.Diff(Build(Circles(0)), Build(Circles(300)));

        Assert.Empty(plain.Changes);
        Assert.Equal(20, Assert.Single(plain.Rollups).Absorbed);
        Assert.Equal(18, Assert.Single(result.Rollups).Absorbed);
        Assert.NotEmpty(result.Changes);
    }

    [Theory]
    [InlineData("B|200:100|300:100", "B|200:100|300:100|300:200", true)] // Only added to.
    [InlineData("B|200:100|300:100", "B|200:100|300:101", true)]
    [InlineData("B|200:100|300:100", "L|200:100|300:100", true)]
    [InlineData("B|200:100|300:100", "B|200:100|300:100", false)]
    public void Shape_IsTheTypeAndEveryNode(string before, string after, bool expected)
    {
        Beatmap With(string shape) =>
            Build(
                "50,50,500,1,0,0:0:0:0:",
                $"100,100,1000,2,0,{shape},1,140",
                "50,50,9000,1,0,0:0:0:0:"
            );

        Assert.Equal(
            expected,
            BeatmapDiffer
                .Diff(With(before), With(after))
                .Changes.Any(change => change.Field == "Shape")
        );
    }

    [Fact]
    public void Files_AreCountedByWhatHappenedToThem()
    {
        using var context = Create();
        context.WriteFile("gone.png", "a");
        context.WriteFile("gone too.png", "b");
        context.WriteFile("changed.png", "c");
        var before = Capture(context, 0, Circles(3));

        File.Delete(Path.Combine(context.SongPath, "gone.png"));
        File.Delete(Path.Combine(context.SongPath, "gone too.png"));
        context.WriteFile("changed.png", "something else");
        context.WriteFile("new 1.png", "1");
        context.WriteFile("new 2.png", "22");
        context.WriteFile("new 3.png", "333");
        var after = Capture(context, 60, Circles(4));

        var counts = SnapshotComparer.Compare("set-12345", before, after).General.Counts;

        Assert.Equal((3, 2, 1), (counts.Added, counts.Removed, counts.Changed));
    }

    [Fact]
    public void BeatmapIdOfZero_IsNone()
    {
        using var context = Create();

        Assert.Null(Assert.Single(Capture(context, 0, Circles(3), 0).Difficulties).BeatmapId);
        Assert.Equal(
            9ul,
            Assert.Single(Capture(context, 60, Circles(4), 9).Difficulties).BeatmapId
        );
    }
}
