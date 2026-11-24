using MapsetVerifier.Checks.Standard.Spread;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckStackLeniencyTests
{
    [Fact]
    public void NoBrokenStacks_NoIssues()
    {
        using var context = CheckTestContext.Create("BrokenStacks");

        var issues = context.RunBeatmapCheck<CheckStackLeniency>("broken stack with hr");

        Assert.Empty(issues);
    }

    [Fact]
    public void BrokenStackHighAr_Ar10_TriggersStackLeniencyIssue()
    {
        using var context = CheckTestContext.Create("BrokenStacks");

        var issues = context.RunBeatmapCheck<CheckStackLeniency>("broken stack ar10");

        Assert.NotEmpty(issues);
        Assert.Contains(
            issues,
            issue =>
                issue.message.Contains(
                    "Stack leniency should be at least",
                    StringComparison.OrdinalIgnoreCase
                ) || issue.message.Contains("Failed stack", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(6, false)]
    public void AlmostStackedObjects_AreReported(int distance, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .CircleSize(4)
                .AR(10)
                .StackLeniency(0.1f)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, 100, 100),
                    TestHitObjects.Circle(1300, 100 + distance, 100)
                )
        );

        var issues = context.RunBeatmapCheck<CheckStackLeniency>("Test");

        Assert.Equal(expected, issues.Count > 0);
        Assert.All(issues, issue => Assert.Contains("ailed stack", issue.message));
    }
}
