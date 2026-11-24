using MapsetVerifier.Checks.AllModes.General.Resources;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckDuplicateStoryboardImagesTests
{
    [Fact]
    public void DifferentImages_ProduceNoIssues()
    {
        using var context = CreateContext([1, 2, 3], [4, 5, 6]);

        Assert.Empty(context.RunGeneralCheck<CheckDuplicateStoryboardImages>());
    }

    [Fact]
    public void IdenticalImages_AreReported()
    {
        using var context = CreateContext([1, 2, 3], [1, 2, 3]);

        var issue = Assert.Single(context.RunGeneralCheck<CheckDuplicateStoryboardImages>());
        Assert.Contains("\"sb/a.png\", \"sb/b.png\" are the same image", issue.message);
    }

    private static CheckTestContext CreateContext(byte[] first, byte[] second) =>
        CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events(
                            "Sprite,Foreground,Centre,\"sb/a.png\",320,240",
                            " F,0,0,1000,1,0",
                            "Sprite,Foreground,Centre,\"sb/b.png\",320,240",
                            " F,0,0,1000,1,0"
                        )
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: [("sb/a.png", first), ("sb/b.png", second)]
        );

    [Fact]
    public void ImagesOfTheSetsStoryboard_AreComparedToo()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events("Sprite,Pass,Centre,\"a.png\",320,240")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFileContents:
            [
                (
                    "MapsetVerifier - MapsetVerifier (Tests).osb",
                    "[Events]\nAnimation,Pass,Centre,\"b.png\",320,240,1,100,LoopForever"
                ),
            ],
            extraBinaryFiles: [("a.png", TestMedia.Png(9, 9)), ("b0.png", TestMedia.Png(9, 9))]
        );

        Assert.NotEmpty(context.RunGeneralCheck<CheckDuplicateStoryboardImages>());
    }
}
