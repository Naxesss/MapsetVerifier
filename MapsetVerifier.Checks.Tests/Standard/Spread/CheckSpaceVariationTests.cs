using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

public class CheckSpaceVariationTests
{
    /// <summary> Circles the given times and distances after one another, along a line. </summary>
    private static List<Issue> Run(params (int Ms, int Distance)[] steps)
    {
        var (time, x) = (1000, 0);
        var hitObjects = new List<string> { TestHitObjects.Circle(time, x) };
        foreach (var (ms, distance) in steps)
            hitObjects.Add(TestHitObjects.Circle(time += ms, x += distance));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().WithDefaultTiming().HitObjects(hitObjects)
        );

        return context.RunBeatmapCheck<CheckSpaceVariation>("Test");
    }

    [Fact]
    public void SameSpacingForASnap_IsFine()
    {
        Assert.Empty(Run((500, 100), (500, 100), (500, 110), (500, 90)));
    }

    [Fact]
    public void OtherSpacingForASnap_IsAWarning_NamingWhatIsExpected()
    {
        var issue = Assert.Single(Run((500, 100), (500, 100), (500, 180)));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("Distance is 180 px, expected 100", issue.message);
    }

    [Fact]
    public void NewSpacingThatIsKept_IsOnlyMentionedOnce()
    {
        Assert.Single(Run((500, 100), (500, 100), (500, 180), (500, 180), (500, 180)));
    }

    [Fact]
    public void OtherSnap_ShouldHaveTheSameRatio()
    {
        Assert.Empty(Run((500, 100), (500, 100), (250, 50)));

        var issue = Assert.Single(Run((500, 100), (500, 100), (250, 150)));
        Assert.Contains("ratio is", issue.message);
    }

    [Fact]
    public void StacksAndFarApartObjects_AreIgnored()
    {
        Assert.Empty(Run((500, 100), (500, 0), (500, 5), (500, 100)));
        Assert.Empty(Run((500, 100), (500, 100), (700, 300), (500, 100)));
    }

    [Fact]
    public void SpacingLongAgo_IsForgotten()
    {
        Assert.Empty(Run((500, 100), (4500, 100), (500, 180)));
    }
}
