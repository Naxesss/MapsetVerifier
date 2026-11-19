using MapsetVerifier.Checks.Catch.Settings;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Settings;

public class CheckOverallDifficultyCatchTests
{
    [Fact]
    public void SameOdAndAr_ProducesNoIssues()
    {
        using var context = CreateContext(od: 8, ar: 8);

        Assert.Empty(context.RunBeatmapCheck<CheckOverallDifficultyCatch>("Test"));
    }

    [Fact]
    public void DifferentOdAndAr_IsReported()
    {
        using var context = CreateContext(od: 7.5f, ar: 8);

        var issue = Assert.Single(context.RunBeatmapCheck<CheckOverallDifficultyCatch>("Test"));
        Assert.Contains("Overall difficulty is 7.5, while approach rate is 8", issue.message);
    }

    private static CheckTestContext CreateContext(float od, float ar) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .OD(od)
                .AR(ar)
                .WithDefaultTiming()
                .WithDefaultHitObject()
        );
}
