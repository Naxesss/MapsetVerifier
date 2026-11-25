using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckFirstLineTests
{
    private static List<Issue> Run(params string[] timingPoints)
    {
        // Standard objects need a line to be parsed, so there are none.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().TimingPoints(timingPoints)
        );

        return context.RunBeatmapCheck<CheckFirstLine>("Test");
    }

    [Fact]
    public void RedLineWithoutKiai_IsFine()
    {
        Assert.Empty(Run(TestTimingPoints.Uninherited(0, 500)));
    }

    [Fact]
    public void GreenLineFirst_IsAProblem()
    {
        var issue = Assert.Single(
            Run(TestTimingPoints.Inherited(0, -100), TestTimingPoints.Uninherited(500, 500))
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("inherited", issue.message);
    }

    [Fact]
    public void GreenLineOnTheRedLine_ComesAfterIt()
    {
        Assert.Empty(
            Run(TestTimingPoints.Inherited(0, -100), TestTimingPoints.Uninherited(0, 500))
        );
    }

    [Fact]
    public void KiaiFromTheFirstLine_IsAProblem()
    {
        var issue = Assert.Single(Run(TestTimingPoints.Uninherited(0, 500, effects: 1)));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("kiai", issue.message);
    }

    [Fact]
    public void NoLines_IsAProblem()
    {
        Assert.Equal(Issue.Level.Problem, Assert.Single(Run()).level);
    }
}
