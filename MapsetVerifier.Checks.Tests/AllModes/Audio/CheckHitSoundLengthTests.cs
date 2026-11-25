using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckHitSoundLengthTests
{
    [Theory]
    [InlineData(24, true)]
    [InlineData(26, false)]
    [InlineData(0, false)] // Blank ones are fine.
    public void ShorterThan25Ms_IsAProblem(double length, bool expected)
    {
        using var context = HitSoundContext.Create(TestMedia.Wave(length, TestMedia.Level(0.5)));

        var issues = context.RunGeneralCheck<CheckHitSoundLength>();

        Assert.Equal(expected, issues.Count == 1);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
    }

    [Fact]
    public void BrokenFile_CouldNotBeChecked()
    {
        using var context = HitSoundContext.Create([1, 2, 3]);

        Assert.Equal(
            Issue.Level.Error,
            Assert.Single(context.RunGeneralCheck<CheckHitSoundLength>()).level
        );
    }
}
