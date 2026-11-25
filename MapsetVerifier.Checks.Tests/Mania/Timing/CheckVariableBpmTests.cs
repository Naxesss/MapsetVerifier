using MapsetVerifier.Checks.Mania.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Timing;

public class CheckVariableBpmTests
{
    private static List<Issue> Run(params string[] timingPoints)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version("Insane")
                .TimingPoints(timingPoints)
                .HitObjects(TestHitObjects.Circle(0), TestHitObjects.Circle(60000))
        );

        return context.RunBeatmapSetCheck<CheckVariableBpm>();
    }

    // Mostly 120 bpm, with 240 bpm from 40 to 45 seconds.
    private static readonly string Base = TestTimingPoints.Uninherited(0, 500);
    private static readonly string Fast = TestTimingPoints.Uninherited(40000, 250);
    private static readonly string BaseAgain = TestTimingPoints.Uninherited(45000, 500);
    private static readonly string End = TestTimingPoints.Inherited(50000, -100);

    [Fact]
    public void ConstantBpm_IsFine()
    {
        Assert.Empty(Run(Base, End));
    }

    [Fact]
    public void Normalised_IsFine()
    {
        Assert.Empty(Run(Base, Fast, TestTimingPoints.Inherited(40000, -200), BaseAgain, End));
    }

    [Fact]
    public void WithoutAGreenLine_IsAWarning_WithTheMultiplier()
    {
        var issue = Assert.Single(Run(Base, Fast, BaseAgain, End));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("multiplier of 0.5", issue.message.Replace(',', '.'));
    }

    [Fact]
    public void WrongMultiplier_IsAWarning()
    {
        Assert.Contains(
            "Isn't normalized",
            Assert
                .Single(Run(Base, Fast, TestTimingPoints.Inherited(40000, -100), BaseAgain, End))
                .message
        );
    }

    [Fact]
    public void MovedGreenLine_IsAProblem()
    {
        var issue = Assert.Single(
            Run(Base, Fast, TestTimingPoints.Inherited(40100, -200), BaseAgain, End)
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("40000", issue.message);
    }

    [Fact]
    public void LastRedLine_IsCheckedToo()
    {
        Assert.Contains("normalizing green line", Assert.Single(Run(Base, Fast)).message);
    }
}
