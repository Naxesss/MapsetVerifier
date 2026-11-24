using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckHasSpinnerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithoutASpinner_IsMinor(bool hasSpinner)
    {
        var builder = new OsuBuilder()
            .Mode(Beatmap.Mode.Catch)
            .WithDefaultTiming()
            .WithDefaultHitObject();
        if (hasSpinner)
            builder.HitObjects(TestHitObjects.Spinner(3000, 5000));

        using var context = CheckTestContext.CreateFromOsu(builder);
        var issues = context.RunBeatmapCheck<CheckHasSpinner>("Test");

        Assert.Equal(hasSpinner, issues.Count == 0);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Minor, issue.level));
    }
}
