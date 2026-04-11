using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckHardRockPerfectStacksTests
{
    [Fact]
    public void BrokenStackHighAr_TriggersWarningForPerfectHardRockStack()
    {
        using var context = CheckTestContext.Create("BrokenStacks");

        var issues = context.RunBeatmapCheck<CheckHardRockPerfectStacks>("broken stack with hr");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("perfect stack", issue.message, StringComparison.OrdinalIgnoreCase);
    }
}