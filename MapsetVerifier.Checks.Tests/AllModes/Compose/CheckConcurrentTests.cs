using MapsetVerifier.Checks.AllModes.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Compose;

public class CheckConcurrentTests
{
    private static List<Issue> Run(Beatmap.Mode mode, params string[] hitObjects)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Mode(mode).WithDefaultTiming().HitObjects(hitObjects)
        );

        return context.RunBeatmapCheck<CheckConcurrent>("Test");
    }

    [Theory]
    [InlineData(1000, "Concurrent Circles")]
    [InlineData(1001, "Within 1 ms")]
    [InlineData(1010, "Within 10 ms")]
    [InlineData(1011, null)]
    public void Circles(int second, string? expected)
    {
        var issues = Run(
            Beatmap.Mode.Standard,
            TestHitObjects.Circle(1000),
            TestHitObjects.Circle(second)
        );

        if (expected == null)
        {
            Assert.Empty(issues);
            return;
        }

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains(expected, issue.message);
    }

    [Fact]
    public void CircleDuringASpinner_IsConcurrent()
    {
        var issue = Assert.Single(
            Run(
                Beatmap.Mode.Standard,
                TestHitObjects.Spinner(1000, 3000),
                TestHitObjects.Circle(2000)
            )
        );

        Assert.Contains("Concurrent Spinner and Circle", issue.message);
    }

    [Fact]
    public void EveryObjectDuringALongOne_IsConcurrent()
    {
        var issues = Run(
            Beatmap.Mode.Standard,
            TestHitObjects.Spinner(1000, 5000),
            TestHitObjects.Circle(2000),
            TestHitObjects.Circle(3000),
            TestHitObjects.Circle(6000)
        );

        Assert.Equal(2, issues.Count);
    }

    [Fact]
    public void SpinnerRightAfterACircle_IsFine()
    {
        Assert.Empty(
            Run(
                Beatmap.Mode.Standard,
                TestHitObjects.Circle(1000),
                TestHitObjects.Spinner(1001, 3000)
            )
        );
    }

    [Theory]
    [InlineData(ManiaColumn1, 1000, true)]
    [InlineData(ManiaColumn1, 1030, true)]
    [InlineData(ManiaColumn1, 1031, false)]
    [InlineData(ManiaColumn2, 1000, false)]
    [InlineData(ManiaColumn2, 1010, false)]
    public void Mania_OnlyWithinAColumn(int column, int time, bool expected)
    {
        var issues = Run(
            Beatmap.Mode.Mania,
            TestHitObjects.Circle(1000, x: ManiaColumn1),
            TestHitObjects.Circle(time, x: column)
        );

        Assert.Equal(expected, issues.Count == 1);
    }

    [Fact]
    public void Mania_NoteWithinAHoldNote_IsConcurrent_PastOtherColumns()
    {
        var issue = Assert.Single(
            Run(
                Beatmap.Mode.Mania,
                $"{ManiaColumn1},192,1000,128,0,5000:0:0:0:0:",
                TestHitObjects.Circle(2000, x: ManiaColumn2),
                TestHitObjects.Circle(4000, x: ManiaColumn1)
            )
        );

        Assert.Contains("Concurrent Hold note and Circle", issue.message);
    }

    private const int ManiaColumn1 = 64;
    private const int ManiaColumn2 = 192;
}
