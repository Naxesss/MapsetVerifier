using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Diffing;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

public class BeatmapDifferTests
{
    private static Beatmap Build(
        string[] hitObjects,
        string timing = "0,500,4,2,0,100,1,0",
        string mode = "0"
    )
    {
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: " + mode,
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Test",
            "BeatmapID:1",
            "BeatmapSetID:1",
            "[Difficulty]",
            "CircleSize:4",
            "SliderMultiplier:1.4",
            "[Events]",
            "[TimingPoints]",
        };
        lines.AddRange(timing.Split('|'));
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects);

        return new Beatmap(string.Join("\n", lines), "song", "test.osu");
    }

    private static string Circle(int time, int x = 100, int y = 100, int hitSound = 0) =>
        $"{x},{y},{time},1,{hitSound},0:0:0:0:";

    private static string[] Row(int count, int start = 1000, int step = 500) =>
        Enumerable.Range(0, count).Select(i => Circle(start + i * step, 50 + i * 20)).ToArray();

    [Fact]
    public void IdenticalBeatmaps_HaveNoChanges()
    {
        var result = BeatmapDiffer.Diff(Build(Row(6)), Build(Row(6)));

        Assert.Empty(result.Changes);
        Assert.Empty(result.Rollups);
    }

    [Fact]
    public void MovedObject_IsOnePlacementChange_NotARemoval()
    {
        var after = Row(6);
        after[2] = Circle(2000, 200, 150);

        var result = BeatmapDiffer.Diff(Build(Row(6)), Build(after));

        var change = Assert.Single(result.Changes);
        Assert.Equal(ChangeKind.Placement, change.Kind);
        Assert.Equal(ChangeOp.Changed, change.Op);
        Assert.Equal(2000, change.Time);
    }

    [Fact]
    public void TinyMove_IsMinor()
    {
        var after = Row(6);
        after[2] = Circle(2000, 91, 100);

        var change = Assert.Single(BeatmapDiffer.Diff(Build(Row(6)), Build(after)).Changes);

        Assert.True(change.Minor);
    }

    [Fact]
    public void UniformOffsetShift_IsOneRollup_AndNoPerObjectChanges()
    {
        var shifted = Enumerable
            .Range(0, 20)
            .Select(i => Circle(1006 + i * 500, 50 + i * 10))
            .ToArray();
        var original = Enumerable
            .Range(0, 20)
            .Select(i => Circle(1000 + i * 500, 50 + i * 10))
            .ToArray();

        var result = BeatmapDiffer.Diff(
            Build(original, "0,500,4,2,0,100,1,0"),
            Build(shifted, "6,500,4,2,0,100,1,0")
        );

        Assert.Empty(result.Changes);
        var rollup = Assert.Single(result.Rollups);
        Assert.Equal(RollupKind.Offset, rollup.Kind);
        Assert.Equal(6, rollup.Amount);
        Assert.Equal(21, rollup.Absorbed);
    }

    [Fact]
    public void AddedAndRemovedObjects_AreReported()
    {
        var after = Row(6).Where((_, i) => i != 3).Append(Circle(5000, 300, 300)).ToArray();

        var result = BeatmapDiffer.Diff(Build(Row(6)), Build(after));

        Assert.Contains(result.Changes, c => c.Op == ChangeOp.Removed && c.Time == 2500);
        Assert.Contains(result.Changes, c => c.Op == ChangeOp.Added && c.Time == 5000);
    }

    [Fact]
    public void HitsoundOnlyChange_IsHitsoundKind()
    {
        var after = Row(6);
        after[1] = Circle(1500, 70, 100, hitSound: 2);

        var change = Assert.Single(BeatmapDiffer.Diff(Build(Row(6)), Build(after)).Changes);

        Assert.Equal(ChangeKind.Hitsound, change.Kind);
    }

    [Fact]
    public void Taiko_ChangingADonIntoAKat_IsAChangeToTheNote_NotAHitsound()
    {
        var after = Row(6);
        after[2] = Circle(2000, 90, 100, hitSound: 2); // whistle: a kat, in the same place

        var change = Assert.Single(
            BeatmapDiffer.Diff(Build(Row(6), mode: "1"), Build(after, mode: "1")).Changes
        );

        Assert.Equal(ChangeKind.Placement, change.Kind);
        Assert.Equal("Note", change.Field);
        Assert.Equal("Don", change.Before);
        Assert.Equal("Kat", change.After);
    }

    [Fact]
    public void Taiko_AddingFinishToAKat_MakesItBig()
    {
        var before = Row(6);
        before[1] = Circle(1500, 70, 100, hitSound: 2);
        var after = Row(6);
        after[1] = Circle(1500, 70, 100, hitSound: 6); // whistle + finish

        var change = Assert.Single(
            BeatmapDiffer.Diff(Build(before, mode: "1"), Build(after, mode: "1")).Changes
        );

        Assert.Equal("Kat", change.Before);
        Assert.Equal("Big kat", change.After);
    }

    [Fact]
    public void Taiko_AddingAClapToAKat_StaysAHitsound()
    {
        var before = Row(6);
        before[1] = Circle(1500, 70, 100, hitSound: 2);
        var after = Row(6);
        after[1] = Circle(1500, 70, 100, hitSound: 10); // whistle + clap: still a kat

        var change = Assert.Single(
            BeatmapDiffer.Diff(Build(before, mode: "1"), Build(after, mode: "1")).Changes
        );

        Assert.Equal(ChangeKind.Hitsound, change.Kind);
    }

    [Fact]
    public void SvChange_IsTimingChange()
    {
        var result = BeatmapDiffer.Diff(
            Build(Row(2), "0,500,4,2,0,100,1,0|1000,-100,4,2,0,100,0,0"),
            Build(Row(2), "0,500,4,2,0,100,1,0|1000,-125,4,2,0,100,0,0")
        );

        var change = Assert.Single(result.Changes);
        Assert.Equal("Sv", change.Field);
        Assert.Equal(ChangeKind.Timing, change.Kind);
    }

    [Fact]
    public void Hunks_SplitOnGaps_AndLabelRemaps()
    {
        var before = Row(8);
        var after = Enumerable
            .Range(0, 8)
            .Select(i => Circle(1000 + i * 500 + 250, 400 - i * 20, 300))
            .ToArray();
        var far = Row(1, start: 20000);

        var result = BeatmapDiffer.Diff(
            Build(before.Concat(far).ToArray()),
            Build(after.Concat(new[] { Circle(20000, 300, 300) }).ToArray())
        );
        var hunks = HunkBuilder.Build(result.Changes);

        Assert.Equal(HunkLabel.Remapped, hunks[0].Label);
        Assert.True(hunks.Count >= 2);
        Assert.True(hunks[0].End < hunks[^1].Start);
    }

    [Fact]
    public void SliderSoundFieldsLeftOutOrWrittenAsZeros_AreNotAChange()
    {
        var left = Build(new[] { "128,288,1000,2,0,L|112:188,1,85" });
        var zeros = Build(new[] { "128,288,1000,2,0,L|112:188,1,85,0|0,0:0|0:0,0:0:0:0:" });
        var whistle = Build(new[] { "128,288,1000,2,0,L|112:188,1,85,0|2,0:0|0:0,0:0:0:0:" });

        Assert.Empty(BeatmapDiffer.Diff(left, zeros).Changes);
        Assert.Contains(BeatmapDiffer.Diff(left, whistle).Changes, c => c.Field == "SliderSamples");
    }
}
