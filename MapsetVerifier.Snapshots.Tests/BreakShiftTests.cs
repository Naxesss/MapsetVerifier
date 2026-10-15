using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots;
using MapsetVerifier.Snapshots.Objects;
using MapsetVerifier.Snapshots.Translators;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

public class BreakShiftTests
{
    [Fact]
    public void GroupsUniformShiftsAndKeepsBoundaryEditsAndOtherEvents()
    {
        var background = Diff("0,0,\"background.jpg\",0,0", Snapshotter.DiffType.Added);
        var changes = new List<DiffInstance>
        {
            Diff("2,1000,2000", Snapshotter.DiffType.Removed),
            Diff("2,1016,2016", Snapshotter.DiffType.Added),
            Diff("2,3000,4000", Snapshotter.DiffType.Removed),
            Diff("2,3016,4016", Snapshotter.DiffType.Added),
            Diff("2,5000,6000", Snapshotter.DiffType.Removed),
            Diff("2,5000,6016", Snapshotter.DiffType.Added),
            background,
        };

        var summary = ShiftedEventsTranslator.GroupShiftedBreaks(changes, 16);

        Assert.NotNull(summary);
        Assert.Equal("2 breaks shifted in time by +16 ms.", summary.Diff);
        Assert.Equal("Timing", summary.Section);
        Assert.Equal(2, summary.Details.Count);
        Assert.Equal(3, changes.Count);
        Assert.Contains(background, changes);

        var residuals = new EventsTranslator().Translate(changes, Beatmap()).ToList();
        Assert.Contains(residuals, diff => diff.Diff.Contains("now ends at"));
        Assert.DoesNotContain(
            residuals,
            diff => diff.Diff.Contains("Break") && diff.DiffType != Snapshotter.DiffType.Changed
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void DoesNotGroupBreaksThatDoNotMatchTheKnownShift(double shift)
    {
        var changes = ShiftedBreak();
        Assert.Null(ShiftedEventsTranslator.GroupShiftedBreaks(changes, shift));
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void MatchesNegativeShiftsWithoutReusingRemovedBreaks()
    {
        var changes = ShiftedBreak();
        changes.Add(Diff("2,1000,2000", Snapshotter.DiffType.Added));
        changes.Add(Diff("2,1000,2000", Snapshotter.DiffType.Added));
        changes.RemoveAt(0);
        changes.Insert(0, Diff("2,1016,2016", Snapshotter.DiffType.Removed));

        var summary = ShiftedEventsTranslator.GroupShiftedBreaks(changes, -16);

        Assert.NotNull(summary);
        Assert.Single(summary.Details);
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void WithoutASnapshotKeepsTheOriginalAddAndRemoveReporting()
    {
        var result = new ShiftedEventsTranslator().Translate(ShiftedBreak(), Beatmap()).ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, diff => diff.DiffType == Snapshotter.DiffType.Added);
        Assert.Contains(result, diff => diff.DiffType == Snapshotter.DiffType.Removed);
    }

    private static List<DiffInstance> ShiftedBreak() =>
        [
            Diff("2,1000,2000", Snapshotter.DiffType.Removed),
            Diff("2,1016,2016", Snapshotter.DiffType.Added),
        ];

    private static DiffInstance Diff(string code, Snapshotter.DiffType type) =>
        new(code, "Events", type, [], DateTime.UnixEpoch);

    private static Beatmap Beatmap() =>
        new(
            "osu file format v14\n[General]\nMode: 0\n[Metadata]\nTitle:Test\nVersion:Test\n"
                + "[Difficulty]\nSliderMultiplier:1.4\nSliderTickRate:1\n[TimingPoints]\n0,500,4,2,0,100,1,0\n[HitObjects]\n",
            "",
            "test.osu"
        );
}
