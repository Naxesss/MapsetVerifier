using MapsetVerifier.Checks.AllModes.HitSounds;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.HitSounds;

public class CheckMutedTests
{
    private static List<Issue> Run(string hitObject, params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().TimingPoints(timingPoints).HitObjects(hitObject)
        );

        return context.RunBeatmapCheck<CheckMuted>("Test");
    }

    [Theory]
    [InlineData(21, null)]
    [InlineData(20, Issue.Level.Minor)]
    [InlineData(11, Issue.Level.Minor)]
    [InlineData(10, Issue.Level.Warning)]
    [InlineData(5, Issue.Level.Warning)]
    public void Circle_ByTheVolumeOfItsLine(int volume, Issue.Level? expected)
    {
        var issues = Run(
            TestHitObjects.Circle(1000),
            TestTimingPoints.Uninherited(0, 500, volume: volume)
        );

        Assert.Equal(expected, issues.SingleOrDefault()?.level);
        Assert.All(issues, issue => Assert.Contains($"{volume}% volume circle", issue.message));
    }

    [Fact]
    public void LessThanFivePercent_PlaysAsFive()
    {
        Assert.Contains(
            "5% volume",
            Assert
                .Single(
                    Run(
                        TestHitObjects.Circle(1000),
                        TestTimingPoints.Uninherited(0, 500, volume: 0)
                    )
                )
                .message
        );
    }

    [Theory]
    [InlineData("0:0:0:5:", 100, true)]
    [InlineData("0:0:0:80:", 5, false)]
    public void VolumeOfTheCircle_OverridesTheLine(string sample, int lineVolume, bool expected)
    {
        var issues = Run(
            TestHitObjects.Circle(1000, hitSample: sample),
            TestTimingPoints.Uninherited(0, 500, volume: lineVolume)
        );

        Assert.Equal(expected, issues.Count == 1);
    }

    [Fact]
    public void LineWithinFiveMsAfter_AlreadyApplies()
    {
        Assert.Empty(
            Run(
                TestHitObjects.Circle(1000),
                TestTimingPoints.Uninherited(0, 500, volume: 5),
                TestTimingPoints.Inherited(1004, -100, volume: 80)
            )
        );
    }

    [Fact]
    public void SilencedSliderTail_IsOnlyMinor()
    {
        // One beat, without any ticks.
        var issue = Assert.Single(
            Run(
                TestHitObjects.Slider(1000, length: 140),
                TestTimingPoints.Uninherited(0, 500, volume: 80),
                TestTimingPoints.Inherited(1400, -100, volume: 5)
            )
        );

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("slider tail", issue.message);
    }

    [Fact]
    public void SilencedReverse_IsAWarning()
    {
        var issue = Assert.Single(
            Run(
                TestHitObjects.Slider(1000, slides: 2, length: 140),
                TestTimingPoints.Uninherited(0, 500, volume: 80),
                TestTimingPoints.Inherited(1400, -100, volume: 5),
                TestTimingPoints.Inherited(1600, -100, volume: 80)
            )
        );

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("slider reverse", issue.message);
    }

    [Fact]
    public void SilencedTick_IsFound_WhateverTheTailsVolume()
    {
        // Two beats with a tick at 1500.
        var issue = Assert.Single(
            Run(
                TestHitObjects.Slider(1000, length: 280),
                TestTimingPoints.Uninherited(0, 500, volume: 80),
                TestTimingPoints.Inherited(1400, -100, volume: 5),
                TestTimingPoints.Inherited(1600, -100, volume: 80)
            )
        );

        Assert.Contains("slider tick", issue.message);
    }

    [Fact]
    public void Spinners_AreIgnored()
    {
        Assert.Empty(
            Run(TestHitObjects.Spinner(1000, 3000), TestTimingPoints.Uninherited(0, 500, volume: 5))
        );
    }

    [Theory]
    [InlineData(50, 0)]
    [InlineData(10, 1)]
    public void SliderReverse_ByTheVolumeOfItsLine(int volume, int expected)
    {
        // Reverses at 1500 ms, and ends at 2000 ms.
        var issues = Run(
            TestHitObjects.Slider(1000, slides: 2, length: 140),
            TestTimingPoints.Uninherited(0, 500),
            TestTimingPoints.Inherited(1400, -100, volume: volume),
            TestTimingPoints.Inherited(1600, -100)
        );

        Assert.Equal(expected, issues.Count(issue => issue.message.Contains("reverse")));
    }

    [Theory]
    [InlineData(50, 0)]
    [InlineData(10, 1)]
    public void SliderTail_ByTheVolumeOfItsLine(int volume, int expected)
    {
        var issues = Run(
            TestHitObjects.Slider(1000, length: 140),
            TestTimingPoints.Uninherited(0, 500),
            TestTimingPoints.Inherited(1400, -100, volume: volume)
        );

        Assert.Equal(expected, issues.Count(issue => issue.message.Contains("tail")));
    }
}
