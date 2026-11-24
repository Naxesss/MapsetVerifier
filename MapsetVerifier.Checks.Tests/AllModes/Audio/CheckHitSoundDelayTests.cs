using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckHitSoundDelayTests
{
    private static List<Issue> Run(Func<double, double> level)
    {
        using var context = HitSoundContext.Create(TestMedia.Wave(100, level));

        return context.RunGeneralCheck<CheckHitSoundDelay>();
    }

    [Fact]
    public void ImmediateImpact_IsFine()
    {
        Assert.Empty(Run(TestMedia.Level(0.5)));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(20)]
    public void SilenceOfFiveMsOrMore_IsAProblem(double silence)
    {
        var issue = Assert.Single(Run(time => time < silence ? 0 : 0.5));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains($"{silence} ms period of complete silence", issue.message);
    }

    [Fact]
    public void QuietStartOfMoreThanFiveMs_IsAWarning()
    {
        var issue = Assert.Single(Run(time => time < 10 ? 0.005 : 0.8));

        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void SilenceOfLessThanFiveMs_IsMinor()
    {
        // "Anything between 1 to 5 ms".
        Assert.Equal(Issue.Level.Minor, Assert.Single(Run(time => time < 3 ? 0 : 0.5)).level);
    }

    [Fact]
    public void SilentFile_HasNothingToDelay()
    {
        Assert.Empty(Run(TestMedia.Level(0)));
    }

    [Fact]
    public void BrokenAndSilentFiles()
    {
        using (var context = HitSoundContext.Create([1, 2, 3]))
            Assert.Equal(
                Issue.Level.Error,
                Assert.Single(context.RunGeneralCheck<CheckHitSoundDelay>()).level
            );

        using (var context = HitSoundContext.Create(TestMedia.Wave(100, TestMedia.Level(0))))
            Assert.Empty(context.RunGeneralCheck<CheckHitSoundDelay>());
    }
}
