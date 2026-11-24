using MapsetVerifier.Checks.AllModes.Events;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Events;

public class CheckBreaksTests
{
    // At AR 5 the preempt is 1200 ms.
    private static List<Issue> Run(string breakLine, params string[] hitObjects)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().AR(5).WithDefaultTiming().Events(breakLine).HitObjects(hitObjects)
        );

        return context.RunBeatmapCheck<CheckBreaks>("Test");
    }

    private static List<Issue> Run(int start, int end) =>
        Run($"2,{start},{end}", TestHitObjects.Circle(1000), TestHitObjects.Circle(10000));

    [Theory]
    [InlineData(1200, 8800)]
    [InlineData(1199, 8801)] // 1 ms of rounding is fine.
    [InlineData(3000, 5000)]
    public void AsTheEditorWouldPlaceIt_IsFine(int start, int end)
    {
        Assert.Empty(Run(start, end));
    }

    [Theory]
    [InlineData(1100, 8800, "starts 100 ms too early.")]
    [InlineData(1200, 8900, "ends 100 ms too late.")]
    [InlineData(1100, 8900, "starts 100 ms too early and ends 100 ms too late.")]
    public void CloserToObjects_IsAProblem(int start, int end, string expected)
    {
        var issue = Assert.Single(Run(start, end));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains(expected, issue.message);
    }

    [Theory]
    [InlineData(3000, 3650, false)]
    [InlineData(3000, 3649, true)]
    public void ShorterThan650Ms_IsAWarning(int start, int end, bool expected)
    {
        var issues = Run(start, end);

        Assert.Equal(expected, issues.Any(issue => issue.level == Issue.Level.Warning));
    }

    [Fact]
    public void ObjectWithinABreak_IsAProblem()
    {
        var issue = Assert.Single(
            Run(
                "2,1200,8800",
                TestHitObjects.Circle(1000),
                TestHitObjects.Circle(5000),
                TestHitObjects.Circle(10000)
            )
        );

        Assert.Contains("ends 5000 ms too late", issue.message);
    }

    [Fact]
    public void Start_IsMeasuredFromTheEndOfTheObjectBefore()
    {
        var issue = Assert.Single(
            Run("2,5050,8800", TestHitObjects.Spinner(1000, 5000), TestHitObjects.Circle(10000))
        );

        Assert.Contains("starts 150 ms too early", issue.message);
    }
}
