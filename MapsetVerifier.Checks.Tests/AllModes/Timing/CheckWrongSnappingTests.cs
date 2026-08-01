using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckWrongSnappingTests
{
    [Theory]
    [InlineData(100, 102, true)]
    [InlineData(100, 103, false)]
    [InlineData(100, 97, false)]
    [InlineData(100, 100, true)]
    public void HasMatchingEdgeWithin_UsesStrictThreeMillisecondTolerance(
        double edgeTime,
        double otherEdgeTime,
        bool expectedMatch
    )
    {
        var sortedOtherEdgeTimes = new List<double> { otherEdgeTime };

        Assert.Equal(
            expectedMatch,
            CheckWrongSnapping.HasMatchingEdgeWithin(sortedOtherEdgeTimes, edgeTime)
        );
    }

    [Fact]
    public void HasMatchingEdgeWithin_FindsMatchInSortedListWithoutScanningEveryEdge()
    {
        var sortedOtherEdgeTimes = Enumerable
            .Range(0, 10_000)
            .Select(index => index * 10d)
            .ToList();

        Assert.True(CheckWrongSnapping.HasMatchingEdgeWithin(sortedOtherEdgeTimes, 50_002));
        Assert.False(CheckWrongSnapping.HasMatchingEdgeWithin(sortedOtherEdgeTimes, 50_003.5));
    }

    [Fact]
    public void GetMissingEdgeTimes_PreservesOriginalEdgeOrder()
    {
        var edgeTimes = new List<double> { 300, 100, 200 };
        var sortedOtherEdgeTimes = new List<double> { 100, 500 };

        var missingEdgeTimes = CheckWrongSnapping.GetMissingEdgeTimes(
            edgeTimes,
            sortedOtherEdgeTimes
        );

        Assert.Equal([300, 200], missingEdgeTimes);
    }

    [Fact]
    public void GetMissingEdgeTimes_ScalesForLargeDiffs()
    {
        var edgeTimes = Enumerable.Range(0, 10_000).Select(index => index * 125d).ToList();
        var sortedOtherEdgeTimes = Enumerable
            .Range(0, 10_000)
            .Select(index => index * 125d + 50)
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var missingEdgeTimes = CheckWrongSnapping.GetMissingEdgeTimes(
            edgeTimes,
            sortedOtherEdgeTimes
        );
        stopwatch.Stop();

        Assert.Equal(10_000, missingEdgeTimes.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500);
    }

    [Fact]
    public void FlagsSnapConsistencyWhenNearbyEdgesFallWithinConfusionRange()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                new OsuBuilder()
                    .Title("Wrong Snapping")
                    .Version("Easy")
                    .OD(4)
                    .TimingPoints("0,600,4,2,0,100,1,0")
                    .HitObjects(
                        "256,192,600,1,0,0:0:0:0:",
                        "256,192,1200,1,0,0:0:0:0:",
                        "256,192,1800,1,0,0:0:0:0:"
                    )
                    .Build()
            ),
            (
                "hard.osu",
                new OsuBuilder()
                    .Title("Wrong Snapping")
                    .Version("Hard")
                    .OD(8)
                    .TimingPoints("0,600,4,2,0,100,1,0")
                    .HitObjects(
                        "256,192,700,1,0,0:0:0:0:",
                        "256,192,1300,1,0,0:0:0:0:",
                        "256,192,1900,1,0,0:0:0:0:",
                        "256,192,2400,1,0,0:0:0:0:",
                        "256,192,3000,1,0,0:0:0:0:",
                        "256,192,3600,1,0,0:0:0:0:",
                        "256,192,4200,1,0,0:0:0:0:",
                        "256,192,4800,1,0,0:0:0:0:"
                    )
                    .Build()
            ),
        ]);

        var easy = context.GetBeatmap("Easy");
        var hard = context.GetBeatmap("Hard");

        Assert.True(easy.StarRating < hard.StarRating);

        var issues = context.RunBeatmapSetCheck<CheckWrongSnapping>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Minor
                && issue.message.Contains("Different snapping")
                && issue.message.Contains("Hard")
        );
    }

    [Fact]
    public void DoesNotFlagWhenDifficultiesShareMatchingEdgeTimes()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "easy.osu",
                new OsuBuilder()
                    .Title("Wrong Snapping")
                    .Version("Easy")
                    .OD(4)
                    .WithDefaultTiming()
                    .HitObjects("256,192,1000,1,0,0:0:0:0:", "256,192,2000,1,0,0:0:0:0:")
                    .Build()
            ),
            (
                "hard.osu",
                new OsuBuilder()
                    .Title("Wrong Snapping")
                    .Version("Hard")
                    .OD(8)
                    .WithDefaultTiming()
                    .HitObjects(
                        "256,192,1000,1,0,0:0:0:0:",
                        "256,192,2000,1,0,0:0:0:0:",
                        "256,192,3000,1,0,0:0:0:0:",
                        "256,192,4000,1,0,0:0:0:0:",
                        "256,192,5000,1,0,0:0:0:0:",
                        "256,192,6000,1,0,0:0:0:0:"
                    )
                    .Build()
            ),
        ]);

        var issues = context
            .RunBeatmapSetCheck<CheckWrongSnapping>()
            .Where(issue => issue.message.Contains("Different snapping"))
            .ToList();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsRareSnapDivisorUsage()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Wrong Snapping")
                .WithDefaultTiming()
                .HitObjects(
                    "256,192,0,1,0,0:0:0:0:",
                    "256,192,500,1,0,0:0:0:0:",
                    "256,192,1000,1,0,0:0:0:0:",
                    "256,192,1500,1,0,0:0:0:0:",
                    "256,192,166.666666666667,1,0,0:0:0:0:"
                )
        );

        var issues = context.RunBeatmapSetCheck<CheckWrongSnapping>();

        Assert.Contains(
            issues,
            issue =>
                issue.message.Contains("1/3")
                && (
                    issue.message.Contains("3 times or less")
                    || issue.message.Contains("0.5% or less")
                )
        );
    }
}
