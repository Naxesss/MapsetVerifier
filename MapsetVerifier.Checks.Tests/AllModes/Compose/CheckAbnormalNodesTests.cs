using MapsetVerifier.Checks.AllModes.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Compose;

public class CheckAbnormalNodesTests
{
    private static List<Issue> Run(int nodes, double length)
    {
        // The head is a node as well.
        var points = string.Join(
            "|",
            Enumerable.Range(1, nodes - 1).Select(i => $"{256 + i % 9}:192")
        );

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Slider(1000, "B", points, length: length))
        );

        return context.RunBeatmapCheck<CheckAbnormalNodes>("Test");
    }

    [Fact]
    public void MoreThanTenTimesTheRootOfTheLength_IsAWarning()
    {
        Assert.Empty(Run(100, 100));

        var issue = Assert.Single(Run(101, 100));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("101 nodes", issue.message);
    }
}
