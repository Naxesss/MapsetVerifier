using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckVideoResolutionTests
{
    [Fact]
    public void MissingVideo_IsAWarning()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Events("Video,0,\"a.mp4\"").WithDefaultTiming().WithDefaultHitObject()
        );

        // Many download without video, so that's expected.
        var issue = Assert.Single(context.RunGeneralCheck<CheckVideoResolution>());

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("missing", issue.message);
    }

    [Fact]
    public void WithoutVideos_ThereIsNothingToCheck()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().WithDefaultTiming().WithDefaultHitObject()
        );

        Assert.Empty(context.RunGeneralCheck<CheckVideoResolution>());
    }
}
