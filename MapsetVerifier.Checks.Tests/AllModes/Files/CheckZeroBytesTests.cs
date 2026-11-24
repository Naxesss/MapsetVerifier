using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckZeroBytesTests
{
    [Fact]
    public void EmptyFiles_AreAProblem_ExceptForDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("test.osu", new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Build()),
                ("empty.osu", ""),
            ],
            ["sb/empty.png"],
            [("full.txt", "text")]
        );

        var issue = Assert.Single(context.RunGeneralCheck<CheckZeroBytes>());

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Equal("\"sb/empty.png\"", issue.message);
    }
}
