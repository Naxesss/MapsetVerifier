using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Timing;

public class CheckEasySliderVelocityTests
{
    private static List<Issue> Run(string version, params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version(version)
                .TimingPoints(timingPoints)
                .HitObjects(TestHitObjects.Circle(0), TestHitObjects.Circle(60000))
        );

        return context.RunBeatmapSetCheck<CheckEasySliderVelocity>();
    }

    [Fact]
    public void SvChange_InAnEasy_IsAProblem()
    {
        var issue = Assert.Single(
            Run("Easy", TestTimingPoints.Uninherited(0, 500), TestTimingPoints.Inherited(5000, -50))
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("multiplier of 2. Consider changing the multiplier to 1.", issue.message);
    }

    [Fact]
    public void Normalisation_OrAHarderDifficulty_IsFine()
    {
        Assert.Empty(
            Run(
                "Easy",
                TestTimingPoints.Uninherited(0, 500),
                TestTimingPoints.Uninherited(40000, 250),
                TestTimingPoints.Inherited(40000, -200),
                TestTimingPoints.Uninherited(45000, 500)
            )
        );
        Assert.Empty(
            Run(
                "Insane",
                TestTimingPoints.Uninherited(0, 500),
                TestTimingPoints.Inherited(5000, -50)
            )
        );
    }
}
