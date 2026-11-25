using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckCommonFinishTests
{
    private static List<Issue> Run(string fileName, int circles, int interval)
    {
        using var context = HitSoundContext.Create(
            TestMedia.Wave(100, TestMedia.Level(0.5)),
            fileName,
            Enumerable
                .Range(0, circles)
                .Select(i => TestHitObjects.Circle(1000 + i * interval, hitSound: 4))
        );

        return context.RunGeneralCheck<CheckCommonFinish>();
    }

    [Fact]
    public void FrequentFinish_IsAWarningWithATimestamp()
    {
        var issue = Assert.Single(Run("soft-hitfinish2.wav", 50, 200));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("leading up to", issue.message);
    }

    [Fact]
    public void FinishEveryFewSeconds_IsCommon()
    {
        Assert.Contains(
            "commonly in [Test]",
            Assert.Single(Run("soft-hitfinish2.wav", 20, 2500)).message
        );
    }

    [Fact]
    public void RareFinish_OrAnotherSound_IsFine()
    {
        Assert.Empty(Run("soft-hitfinish2.wav", 5, 10000));
        Assert.Empty(Run("soft-hitnormal2.wav", 50, 200));
    }
}
