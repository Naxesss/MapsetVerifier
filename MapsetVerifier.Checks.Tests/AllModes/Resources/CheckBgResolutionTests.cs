using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckBgResolutionTests
{
    private static List<Issue> Run(string background, byte[]? content)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events($"0,0,\"{background}\",0,0")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: content == null ? null : [(background, content)]
        );

        return context.RunGeneralCheck<CheckBgResolution>();
    }

    [Theory]
    [InlineData(1920, 1080, null)]
    [InlineData(2560, 1440, null)]
    [InlineData(1024, 640, null)]
    [InlineData(2561, 1440, Issue.Level.Problem)]
    [InlineData(2560, 1441, Issue.Level.Problem)]
    [InlineData(1023, 640, Issue.Level.Warning)]
    [InlineData(1024, 639, Issue.Level.Warning)]
    public void Resolution(int width, int height, Issue.Level? expected)
    {
        var issues = Run("bg.png", TestMedia.Png(width, height));

        Assert.Equal(expected, issues.SingleOrDefault()?.level);
        Assert.All(issues, issue => Assert.Contains($"({width} x {height})", issue.message));
    }

    [Fact]
    public void FileSize_AboveTwoAndAHalfMegabytes_IsAProblem()
    {
        Assert.Empty(Run("bg.png", TestMedia.Png(1920, 1080, 2_600_000)));

        var issue = Assert.Single(Run("bg.png", TestMedia.Png(1920, 1080, 2_700_000)));
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("2.5", issue.message);
    }

    [Fact]
    public void BrokenImage_CouldNotBeChecked()
    {
        Assert.Equal(Issue.Level.Error, Assert.Single(Run("bg.png", [1, 2, 3])).level);
    }

    [Fact]
    public void MissingImage_IsAProblem()
    {
        var issue = Assert.Single(Run("bg.png", null));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("missing", issue.message);
    }

    [Fact]
    public void OutsideOfTheFolder_IsAProblem()
    {
        var issue = Assert.Single(Run("../bg.png", null));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("leaves the current song folder", issue.message);
    }
}
