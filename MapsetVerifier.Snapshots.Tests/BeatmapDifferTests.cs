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

    /// <summary> 40 objects 500 ms apart, with the ones from index <c>from</c> to <c>to</c> moved. </summary>
    private static string[] WithMoved(params (int From, int To, int Delta)[] moves) =>
        Enumerable
            .Range(0, 40)
            .Select(i =>
                Circle(
                    1000 + i * 500 + moves.Where(m => i >= m.From && i <= m.To).Sum(m => m.Delta),
                    50 + i * 10
                )
            )
            .ToArray();

    [Fact]
    public void PartsMovedOnTheirOwn_AreOneShiftEach_WithTheirTimingLines()
    {
        // Two parts, each starting at a red line with a green line inside, moved by different
        // amounts; the rest stays. Like moving a section's timing to fix its offset.
        var before = Build(
            WithMoved(),
            "0,500,4,2,0,100,1,0|5000,500,4,2,0,100,1,0|6000,-50,4,2,0,100,0,0|"
                + "8000,500,4,2,0,100,1,0|10000,500,4,2,0,100,1,0|12000,500,4,2,0,100,1,0"
        );
        var after = Build(
            WithMoved((8, 13, 30), (22, 25, -12)),
            "0,500,4,2,0,100,1,0|5030,500,4,2,0,100,1,0|6030,-50,4,2,0,100,0,0|"
                + "8000,500,4,2,0,100,1,0|10000,500,4,2,0,100,1,0|11988,500,4,2,0,100,1,0"
        );

        var result = BeatmapDiffer.Diff(before, after);

        Assert.Empty(result.Rollups);
        Assert.All(result.Changes, c => Assert.Equal("Shift", c.Field));
        Assert.Collection(
            result.Changes.OrderBy(c => c.Time),
            c =>
            {
                Assert.Equal(30, c.Magnitude);
                Assert.Equal(6 + 2, c.Count);
            },
            c =>
            {
                Assert.Equal(-12, c.Magnitude);
                Assert.Equal(4 + 1, c.Count);
            }
        );
    }

    [Fact]
    public void TwoPartsMovedByTheSameAmount_StayTwoShifts()
    {
        var result = BeatmapDiffer.Diff(
            Build(WithMoved()),
            Build(WithMoved((5, 9, 20), (25, 29, 20)))
        );

        Assert.Empty(result.Rollups);
        Assert.Equal(2, result.Changes.Count(c => c.Field == "Shift"));
        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public void APartMovedOnTopOfAnOffsetChange_IsAShiftOfItsOwnAmount()
    {
        var result = BeatmapDiffer.Diff(
            Build(WithMoved()),
            Build(WithMoved((0, 39, 10), (20, 24, 30)), "10,500,4,2,0,100,1,0")
        );

        Assert.Equal(10, Assert.Single(result.Rollups).Amount);
        Assert.Equal(30, Assert.Single(result.Changes, c => c.Field == "Shift").Magnitude);
    }

    [Fact]
    public void TwoObjectsMovedTogether_AreRetimes_NotAShift()
    {
        var result = BeatmapDiffer.Diff(Build(WithMoved()), Build(WithMoved((10, 11, 20))));

        Assert.DoesNotContain(result.Changes, c => c.Field == "Shift");
        Assert.Equal(2, result.Changes.Count(c => c.Field == "Time"));
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
    public void Mania_AHoldNoteTurnedIntoANote_IsOneChange()
    {
        // Regression for set 1223763: the hold note became a note in the same column and two notes
        // were removed, which read as "Remapped: 0 objects added, 2 removed".
        var before = new[]
        {
            "320,192,46878,128,0,47014:0:0:0:0:",
            "192,192,47082,1,0,0:0:0:0:",
            "64,192,47218,1,0,0:0:0:0:",
            "320,192,47355,1,0,0:0:0:0:",
        };
        var after = new[] { "320,192,46878,1,0,0:0:0:0:", "64,192,47218,1,0,0:0:0:0:" };

        var changes = BeatmapDiffer.Diff(Build(before, mode: "3"), Build(after, mode: "3")).Changes;

        var type = Assert.Single(changes, c => c.Field == "Type");
        Assert.Equal(("Hold note", "Note"), (type.Before, type.After));
        Assert.Equal(2, changes.Count(c => c.Op == ChangeOp.Removed));
        Assert.DoesNotContain(changes, c => c.Op == ChangeOp.Added);
    }

    [Theory]
    [InlineData("1", false)]
    [InlineData("3", false)]
    [InlineData("0", true)]
    [InlineData("2", true)]
    public void ANewCombo_IsOnlyAChangeWhereCombosShow(string mode, bool reported)
    {
        // Regression for set 2480591: mania listed "Removed new combo." for a flag it never shows.
        var before = new[] { "64,192,580,5,0,0:0:0:0:", "192,192,790,1,0,0:0:0:0:" };
        var after = new[] { "64,192,580,1,0,0:0:0:0:", "192,192,790,1,0,0:0:0:0:" };

        var changes = BeatmapDiffer
            .Diff(Build(before, mode: mode), Build(after, mode: mode))
            .Changes;

        Assert.Equal(reported, changes.Any(c => c.Field == "NewCombo"));
    }

    [Fact]
    public void ACircleTurnedIntoASliderInPlace_IsOneChange()
    {
        var before = Row(4);
        var after = Row(4);
        after[1] = "70,100,1500,2,0,L|170:100,1,100";

        var change = Assert.Single(BeatmapDiffer.Diff(Build(before), Build(after)).Changes);

        Assert.Equal(("Type", "Circle", "Slider"), (change.Field, change.Before, change.After));
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

    /// <summary> A red line and green lines every 500 ms, with the volumes given. </summary>
    private static string Lines(params int[] volumes) =>
        string.Join(
            "|",
            volumes.Select(
                (v, i) => i == 0 ? $"0,500,4,2,0,{v},1,0" : $"{i * 500},-100,4,2,0,{v},0,0"
            )
        );

    [Fact]
    public void AVolumePassOverMostLines_IsOneRollup()
    {
        var result = BeatmapDiffer.Diff(
            Build(Row(6), Lines(100, 100, 100, 100, 100, 100)),
            Build(Row(6), Lines(100, 80, 80, 80, 80, 80))
        );

        Assert.Empty(result.Changes);
        var rollup = Assert.Single(result.Rollups);
        Assert.Equal(RollupKind.Volume, rollup.Kind);
        Assert.Equal(-20, rollup.Amount);
        Assert.Equal(5, rollup.Absorbed);
        Assert.Equal(("100", "80"), (rollup.Before, rollup.After));
    }

    [Fact]
    public void LinesSetToOneVolume_AreOneGroup_AndAStayedLineEndsIt()
    {
        // 1500 to 3000 all set to 50 (from different volumes); 3500 stays; 4000 and 4500 change
        // too few to be a group of their own.
        var result = BeatmapDiffer.Diff(
            Build(Row(6), Lines(100, 100, 100, 90, 70, 60, 100, 100, 100, 100, 100, 100)),
            Build(Row(6), Lines(100, 100, 100, 50, 50, 50, 50, 100, 40, 40, 100, 100))
        );

        Assert.Empty(result.Rollups);
        var group = Assert.Single(result.Changes, c => c.Field == "VolumeGroup");
        Assert.Equal((1500.0, 3000.0), (group.Time, group.EndTime));
        Assert.Equal(4, group.Count);
        Assert.Null(group.Before);
        Assert.Equal("50", group.After);
        Assert.Equal(2, result.Changes.Count(c => c.Field == "GreenLineVolume"));
    }

    [Fact]
    public void GreenLinesAddedOrRemoved_NameTheirSv()
    {
        var result = BeatmapDiffer.Diff(
            Build(Row(4), "0,500,4,2,0,100,1,0|1000,-50,4,2,0,100,0,0"),
            Build(Row(4), "0,500,4,2,0,100,1,0|2000,-125,4,2,0,100,0,0")
        );

        var removed = Assert.Single(result.Changes, c => c.Op == ChangeOp.Removed);
        var added = Assert.Single(result.Changes, c => c.Op == ChangeOp.Added);
        Assert.Equal(("GreenLine", "2", null), (removed.Field, removed.Before, removed.After));
        Assert.Equal(("GreenLine", null, "0.8"), (added.Field, added.Before, added.After));
    }

    [Fact]
    public void ANewLine_DoesNotEndAVolumeGroup()
    {
        var result = BeatmapDiffer.Diff(
            Build(Row(6), Lines(100, 100, 100, 100)),
            Build(Row(6), Lines(80, 80, 80, 80) + "|1250,-100,4,2,0,80,0,0")
        );

        var rollup = Assert.Single(result.Rollups);
        Assert.Equal(4, rollup.Absorbed);
        Assert.Equal("GreenLine", Assert.Single(result.Changes).Field); // the new line itself
    }

    [Fact]
    public void Taiko_ADrumrollsShapeAndWhereNotesSit_AreNotChanges_ButItsLengthIs()
    {
        var before = Row(4);
        before[1] = "100,100,1500,2,0,L|200:100,1,100";
        var after = Row(4);
        after[1] = "300,250,1500,2,0,P|350:200|400:250,1,100"; // moved and bent, same length
        after[2] = Circle(2000, 400, 300); // moved

        Assert.Empty(BeatmapDiffer.Diff(Build(before, mode: "1"), Build(after, mode: "1")).Changes);

        var longer = Row(4);
        longer[1] = "100,100,1500,2,0,L|300:100,1,200";
        var change = Assert.Single(
            BeatmapDiffer.Diff(Build(before, mode: "1"), Build(longer, mode: "1")).Changes
        );
        Assert.Equal("Length", change.Field);
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
