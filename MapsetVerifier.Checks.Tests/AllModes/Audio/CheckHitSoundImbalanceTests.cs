using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckHitSoundImbalanceTests
{
    private static List<Issue> Run(int circles, int interval, params double[] levels)
    {
        using var context = HitSoundContext.Create(
            TestMedia.Wave(100, levels.Select(TestMedia.Level).ToArray()),
            hitObjects: Enumerable
                .Range(0, circles)
                .Select(i => TestHitObjects.Circle(1000 + i * interval))
        );

        return context.RunGeneralCheck<CheckHitSoundImbalance>();
    }

    [Fact]
    public void BalancedOrMono_IsFine()
    {
        Assert.Empty(Run(50, 200, 0.5, 0.5));
        Assert.Empty(Run(50, 200, 0.5, 0.26));
        Assert.Empty(Run(50, 200, 0.5));
    }

    [Theory]
    [InlineData(0, 0.5, "left")]
    [InlineData(0.5, 0, "right")]
    public void SilentChannel_IsAWarning(double left, double right, string expected)
    {
        var issue = Assert.Single(Run(1, 200, left, right));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"completely silent in the {expected} channel", issue.message);
    }

    [Fact]
    public void LouderChannel_OftenInAShortTime_IsAWarningWithATimestamp()
    {
        var issue = Assert.Single(Run(50, 200, 0.8, 0.2));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("louder left channel", issue.message);
        Assert.Contains("leading up to", issue.message);
    }

    [Fact]
    public void LouderChannel_ThroughoutADifficulty_IsAWarning()
    {
        var issue = Assert.Single(Run(10, 8000, 0.2, 0.8));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("louder right channel", issue.message);
        Assert.Contains("commonly in [Test]", issue.message);
    }

    [Fact]
    public void LouderChannel_RarelyUsed_IsMinor()
    {
        Assert.Equal(Issue.Level.Minor, Assert.Single(Run(2, 60000, 0.8, 0.2)).level);
    }

    [Theory]
    [InlineData(50, 200)]
    [InlineData(2, 60000)]
    public void LouderRightChannel_IsNamedToo(int circles, int interval)
    {
        Assert.Contains(
            "louder right channel",
            Assert.Single(Run(circles, interval, 0.2, 0.8)).message
        );
    }

    [Fact]
    public void LouderLeftChannel_ThroughoutADifficulty_IsNamedToo()
    {
        Assert.Contains("louder left channel", Assert.Single(Run(10, 8000, 0.8, 0.2)).message);
    }

    [Fact]
    public void BrokenFile_CouldNotBeChecked()
    {
        using var context = HitSoundContext.Create([1, 2, 3]);

        Assert.Equal(
            Issue.Level.Error,
            Assert.Single(context.RunGeneralCheck<CheckHitSoundImbalance>()).level
        );
    }

    [Fact]
    public void Mania_CountsHalf_AndOnlyTheDifficultyUsingItIsNamed()
    {
        string Build(string version, int index) =>
            new OsuBuilder()
                .Mode(Parser.Objects.Beatmap.Mode.Mania)
                .Version(version)
                .TimingPoints(TestTimingPoints.Uninherited(0, 500, sampleIndex: index))
                .HitObjects(
                    Enumerable.Range(0, 10).Select(i => TestHitObjects.Circle(1000 + i * 4000, 64))
                )
                .Build();

        using var context = CheckTestContext.CreateFromOsuFiles(
            [("a.osu", Build("Unused", 1)), ("b.osu", Build("Used", 2))],
            extraBinaryFiles:
            [
                (
                    "soft-hitnormal2.wav",
                    TestMedia.Wave(100, TestMedia.Level(0.8), TestMedia.Level(0.2))
                ),
            ]
        );

        var issue = Assert.Single(context.RunGeneralCheck<CheckHitSoundImbalance>());

        Assert.Contains("[Used]", issue.message);
    }
}
