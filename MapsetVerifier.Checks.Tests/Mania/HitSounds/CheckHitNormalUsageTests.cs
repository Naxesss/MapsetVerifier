using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.HitSounds;

public class CheckHitNormalUsageTests
{
    private static List<Issue> Run(string[] files, params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .TimingPoints(timingPoints)
                .WithDefaultHitObject(),
            files
        );

        return context.RunBeatmapSetCheck<CheckHitNormalUsage>();
    }

    [Fact]
    public void WithoutAHitNormalFile_IsAWarning()
    {
        Assert.Equal(
            Issue.Level.Warning,
            Assert.Single(Run([], TestTimingPoints.Uninherited(0, 500, sampleSet: 1))).level
        );
    }

    [Fact]
    public void LinesUsingTheFile_AreFine()
    {
        Assert.Empty(Run(["soft-hitnormal2.wav"], TestTimingPoints.Uninherited(0, 500, 4, 2, 2)));
    }

    [Fact]
    public void LineUsingAnotherSampleset_IsAProblem()
    {
        var issue = Assert.Single(
            Run(
                ["soft-hitnormal2.wav"],
                TestTimingPoints.Uninherited(0, 500, 4, 2, 2),
                TestTimingPoints.Inherited(5000, -100, 4, 3, 2)
            )
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
    }

    [Fact]
    public void LineWithoutASampleset_IsReported()
    {
        Assert.NotEmpty(
            Run(["soft-hitnormal2.wav"], TestTimingPoints.Uninherited(0, 500, 4, 0, 2))
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FirstCustomIndex_HasNoNumber(int index)
    {
        Assert.Empty(
            Run(["soft-hitnormal.wav"], TestTimingPoints.Uninherited(0, 500, 4, 2, index))
        );
    }
}
