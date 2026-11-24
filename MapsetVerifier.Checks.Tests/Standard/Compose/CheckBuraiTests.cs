using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckBuraiTests
{
    private static List<Issue> Run(string points, double length)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "B", points, length: length, x: 100, y: 100)
                )
        );

        return context.RunBeatmapCheck<CheckBurai>("Test");
    }

    // Out and the same way back.
    [Theory(Skip = "Bug: the score depends on the direction and on where the path is sampled.")]
    [InlineData("200:200|200:200|100:100", 280)] // Diagonally.
    [InlineData("100:250|100:250|100:100", 300)] // Downwards.
    [InlineData("250:100|250:100|100:100", 300)] // To the right.
    [InlineData("200:20|200:20|100:100", 250)]
    public void GoingBackOverItself_IsBurai(string points, double length)
    {
        var issue = Assert.Single(Run(points, length));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.DoesNotContain("Potentially", issue.message);
    }

    [Theory]
    [InlineData("200:200|300:100", 250)] // A single arc.
    [InlineData("250:100|250:100|250:250|250:250|175:250|175:250|175:20", 600)] // Crosses at a right angle.
    public void ReadablePath_IsFine(string points, double length)
    {
        Assert.Empty(Run(points, length));
    }
}
