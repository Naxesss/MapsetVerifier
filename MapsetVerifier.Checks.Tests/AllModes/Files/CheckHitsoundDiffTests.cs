using MapsetVerifier.Checks.AllModes.General.Files;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckHitsoundDiffTests
{
    [Theory]
    [InlineData("hitsounds", true)]
    [InlineData("HIT SOUNDS copy", true)]
    [InlineData("_HS_", true)]
    [InlineData("Hard", false)]
    [InlineData("HS Insane", false)]
    public void NameOfAHitSoundDifficulty_IsAProblem(string version, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Version(version).WithDefaultTiming().WithDefaultHitObject()
        );

        Assert.Equal(expected, context.RunBeatmapSetCheck<CheckHitsoundDiff>().Count == 1);
    }
}
