using MapsetVerifier.Checks.AllModes.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Compose;

public class CheckDrainTimeTests
{
    private static List<Issue> Run(int end, string? breakLine = null, params int[] between)
    {
        var builder = new OsuBuilder()
            .WithDefaultTiming()
            .HitObjects(TestHitObjects.Circle(0))
            .HitObjects(between.Select(time => TestHitObjects.Circle(time)))
            .HitObjects(TestHitObjects.Circle(end));

        if (breakLine != null)
            builder.Events(breakLine);

        using var context = CheckTestContext.CreateFromOsu(builder);

        return context.RunBeatmapCheck<CheckDrainTime>("Test");
    }

    [Fact]
    public void ThirtySeconds_IsEnough()
    {
        Assert.Empty(Run(30000));
    }

    [Fact]
    public void LessThanThirtySeconds_IsAProblem_AndIsNotRoundedUp()
    {
        var issue = Assert.Single(Run(29999));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("29 seconds", issue.message);
    }

    [Fact]
    public void Breaks_DoNotCount()
    {
        Assert.Contains(
            "20 seconds",
            Assert.Single(Run(35000, "2,5200,19000", 5000, 20000)).message
        );
    }
}
