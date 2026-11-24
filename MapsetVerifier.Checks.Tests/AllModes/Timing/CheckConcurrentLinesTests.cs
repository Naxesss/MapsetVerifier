using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckConcurrentLinesTests
{
    private static List<Issue> Run(params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().TimingPoints(timingPoints).WithDefaultHitObject()
        );

        return context.RunBeatmapCheck<CheckConcurrentLines>("Test");
    }

    [Fact]
    public void TwoLinesOfTheSameKind_AreAProblem()
    {
        var red = Assert.Single(
            Run(TestTimingPoints.Uninherited(0, 500), TestTimingPoints.Uninherited(0, 400))
        );
        Assert.Equal(Issue.Level.Problem, red.level);
        Assert.Contains("Concurrent uninherited", red.message);

        var green = Assert.Single(
            Run(
                TestTimingPoints.Uninherited(0, 500),
                TestTimingPoints.Inherited(1000, -100),
                TestTimingPoints.Inherited(1000, -50)
            )
        );
        Assert.Equal(Issue.Level.Problem, green.level);
        Assert.Contains("Concurrent inherited", green.message);
    }

    [Fact]
    public void GreenOnRed_WithTheSameSettings_IsFine()
    {
        Assert.Empty(Run(TestTimingPoints.Uninherited(0, 500), TestTimingPoints.Inherited(0, -50)));
    }

    [Fact]
    public void GreenOnRed_WithOtherSettings_NamesOnlyWhatDiffers()
    {
        var issue = Assert.Single(
            Run(
                TestTimingPoints.Uninherited(0, 500, sampleSet: 1, sampleIndex: 2, volume: 80),
                TestTimingPoints.Inherited(0, -100, sampleSet: 3, sampleIndex: 2, volume: 60)
            )
        );

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("Green: 60% volume, Drum sampleset.", issue.message);
        Assert.Contains("Red: 80% volume, Normal sampleset.", issue.message);
        Assert.Contains("Green overrides red", issue.message);
        Assert.DoesNotContain("kiai", issue.message);
        Assert.DoesNotContain("custom", issue.message);
    }

    [Fact]
    public void LinesAMillisecondApart_AreNotConcurrent()
    {
        Assert.Empty(
            Run(TestTimingPoints.Uninherited(0, 500), TestTimingPoints.Uninherited(1, 400))
        );
    }

    [Theory]
    [InlineData(1, 0, 0, "Green: kiai.", "Red: no kiai.")]
    [InlineData(0, 1, 0, "Green: no kiai.", "Red: kiai.")]
    [InlineData(0, 0, 3, "Green: custom 3.", "Red: custom 0.")]
    [InlineData(1, 0, 3, "Green: kiai, custom 3.", "Red: no kiai, custom 0.")]
    public void KiaiAndCustomIndex_AreNamed(
        int greenKiai,
        int redKiai,
        int greenIndex,
        string green,
        string red
    )
    {
        var issue = Assert.Single(
            Run(
                TestTimingPoints.Uninherited(0, 500, effects: redKiai),
                TestTimingPoints.Inherited(0, -100, sampleIndex: greenIndex, effects: greenKiai)
            )
        );

        Assert.Contains(green, issue.message);
        Assert.Contains(red, issue.message);
    }

    [Fact]
    public void EverySetting_IsListedInOrder()
    {
        var issue = Assert.Single(
            Run(
                TestTimingPoints.Uninherited(0, 500, sampleSet: 1, sampleIndex: 1, volume: 80),
                TestTimingPoints.Inherited(
                    0,
                    -100,
                    sampleSet: 3,
                    sampleIndex: 2,
                    volume: 60,
                    effects: 1
                )
            )
        );

        Assert.Contains("Green: kiai, 60% volume, Drum sampleset, custom 2.", issue.message);
        Assert.Contains("Red: no kiai, 80% volume, Normal sampleset, custom 1.", issue.message);
    }

    /// <summary> Only what differs is named. </summary>
    [Theory]
    [InlineData(100, 3, 0, "Soft sampleset", "Drum sampleset")]
    [InlineData(80, 2, 0, "100% volume", "80% volume")]
    [InlineData(100, 2, 5, "custom 0", "custom 5")]
    [InlineData(
        80,
        3,
        5,
        "100% volume, Soft sampleset, custom 0",
        "80% volume, Drum sampleset, custom 5"
    )]
    public void ConflictingSettings_AreNamed(
        int volume,
        int sampleset,
        int index,
        string red,
        string green
    )
    {
        var issue = Assert.Single(
            Run("0,500,4,2,0,100,1,0", $"0,-100,4,{sampleset},{index},{volume},0,0")
        );

        Assert.Contains(red, issue.message);
        Assert.Contains(green, issue.message);
        Assert.DoesNotContain(", " + green, issue.message);
        Assert.DoesNotContain("kiai", issue.message);
    }
}
