using MapsetVerifier.Checks.AllModes.HitSounds;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.HitSounds;

public class CheckSilentHitSoundsTests
{
    private static List<Issue> Run(byte[] file)
    {
        using var context = HitSoundContext.Create(file);

        return context.RunGeneralCheck<CheckSilentHitSounds>();
    }

    [Fact]
    public void SilentFile_IsAProblem()
    {
        var issue = Assert.Single(Run(TestMedia.Wave(100, TestMedia.Level(0))));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("soft-hitnormal2.wav", issue.message);
    }

    [Fact]
    public void AudibleFile_IsFine()
    {
        Assert.Empty(Run(TestMedia.Wave(100, TestMedia.Level(0.05))));
    }

    [Fact]
    public void OfficialBlankFile_IsFine()
    {
        var blank = TestMedia.Wave(0, TestMedia.Level(0));

        Assert.Equal(44, blank.Length);
        Assert.Empty(Run(blank));
    }

    [Fact]
    public void BrokenFile_CouldNotBeChecked()
    {
        Assert.Equal(Issue.Level.Error, Assert.Single(Run([1, 2, 3])).level);
    }
}
