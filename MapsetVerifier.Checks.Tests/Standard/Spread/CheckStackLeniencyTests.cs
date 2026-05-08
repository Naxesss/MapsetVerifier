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
}
