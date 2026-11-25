using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckEmptyDifficultyFileTests
{
    [Fact]
    public void FilesWithoutAnything_AreAProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Build()),
            ("empty.osu", ""),
            ("blank.osu", " \n"),
        ]);

        var issues = context.RunGeneralCheck<CheckEmptyDifficultyFile>();

        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
        Assert.Contains(issues, issue => issue.message.Contains("empty.osu"));
        Assert.Contains(issues, issue => issue.message.Contains("blank.osu"));
    }

    [Fact]
    public void AnyContent_IsFine()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().WithDefaultTiming().WithDefaultHitObject()
        );

        Assert.Empty(context.RunGeneralCheck<CheckEmptyDifficultyFile>());
    }
}
