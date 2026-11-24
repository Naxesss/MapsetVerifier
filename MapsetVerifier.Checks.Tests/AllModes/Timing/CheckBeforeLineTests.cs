using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckBeforeLineTests
{
    private static List<Issue> Run(
        Beatmap.Mode mode,
        string hitObject,
        params string[] timingPoints
    )
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(mode)
                .TimingPoints(TestTimingPoints.Uninherited(0, 500))
                .TimingPoints(timingPoints)
                .HitObjects(hitObject)
        );

        return context.RunBeatmapCheck<CheckBeforeLine>("Test");
    }

    [Theory]
    [InlineData(1001, true)]
    [InlineData(1005, true)]
    [InlineData(1006, false)]
    [InlineData(1000, false)]
    public void Slider_UpToFiveMsBeforeAnSvChange_IsAWarning(int line, bool expected)
    {
        var issues = Run(
            Beatmap.Mode.Standard,
            TestHitObjects.Slider(1000),
            TestTimingPoints.Inherited(line, -50)
        );

        if (!expected)
        {
            Assert.Empty(issues);
            return;
        }

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"{line - 1000} ms before", issue.message);
    }

    [Fact]
    public void LineWithoutAnSvChange_IsFine()
    {
        Assert.Empty(
            Run(
                Beatmap.Mode.Standard,
                TestHitObjects.Slider(1000),
                TestTimingPoints.Inherited(1003, -100)
            )
        );
    }

    [Theory]
    [InlineData(Beatmap.Mode.Standard, false)]
    [InlineData(Beatmap.Mode.Catch, false)]
    // Scroll speed applies to everything.
    [InlineData(Beatmap.Mode.Taiko, true)]
    public void Circle_OnlyMattersWhereItScrolls(Beatmap.Mode mode, bool expected)
    {
        var issues = Run(mode, TestHitObjects.Circle(1000), TestTimingPoints.Inherited(1003, -50));

        Assert.Equal(expected, issues.Count == 1);
    }

    [Fact]
    public void RedLineKeepingTheSpeedOfTheGreenLineOnIt_IsFine()
    {
        // 0.5x before and after, at the same bpm.
        Assert.Empty(
            Run(
                Beatmap.Mode.Standard,
                TestHitObjects.Slider(1000),
                TestTimingPoints.Inherited(500, -200),
                TestTimingPoints.Uninherited(1003, 500),
                TestTimingPoints.Inherited(1003, -200)
            )
        );
    }

    [Fact]
    public void TaikoNote_RightAfterAnSvChange_IsFine()
    {
        // Often deliberate.
        Assert.Empty(
            Run(
                Beatmap.Mode.Taiko,
                TestHitObjects.Circle(1000),
                TestTimingPoints.Inherited(997, -50)
            )
        );
    }

    [Theory]
    [InlineData(Beatmap.Mode.Taiko, "256,192,1000,1,0,0:0:0:0:", "Circle")]
    [InlineData(Beatmap.Mode.Taiko, "256,192,1000,12,0,2000,0:0:0:0:", "Spinner")]
    [InlineData(Beatmap.Mode.Mania, "64,192,1000,128,0,2000:0:0:0:0:", "Hold note")]
    [InlineData(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:", null)]
    [InlineData(Beatmap.Mode.Catch, "256,192,1000,12,0,2000,0:0:0:0:", null)]
    public void OtherObjects_OnlyWhereSvMovesThem(
        Beatmap.Mode mode,
        string hitObject,
        string? expected
    )
    {
        var issues = Run(mode, hitObject, TestTimingPoints.Inherited(1003, -50));

        if (expected == null)
            Assert.Empty(issues);
        else
            Assert.Contains(expected, Assert.Single(issues).message);
    }

    [Fact]
    public void UnsnappedObject_IsLeftToAnotherCheck()
    {
        Assert.Empty(
            Run(
                Beatmap.Mode.Standard,
                TestHitObjects.Slider(1020),
                TestTimingPoints.Inherited(1023, -50)
            )
        );
    }
}
