using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckVideoCodecTests
{
    private static List<Issue> Run(string path, byte[]? video)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events($"Video,0,\"{path}\"")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: video == null ? null : [(path, video)]
        );

        return context.RunGeneralCheck<CheckVideoCodec>();
    }

    [Fact]
    public void UnreadableVideo_IsAWarning()
    {
        var issue = Assert.Single(Run("a.mp4", [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("a.mp4", issue.message);
    }

    [Fact]
    public void MissingOrOutsideOfTheFolder_IsLeftToAnotherCheck()
    {
        Assert.Empty(Run("a.mp4", null));
        Assert.Empty(Run("../a.mp4", null));
    }

    [Theory]
    [InlineData("avc1", null)]
    [InlineData("hvc1", "H.265 / HEVC")]
    [InlineData("vp09", "VP9")]
    public void OtherCodecThanH264_IsAnIssue(string codec, string? expected)
    {
        var issue = Run("a.mp4", TestMedia.Mp4(codec)).SingleOrDefault();

        if (expected == null)
            Assert.Null(issue);
        else
            Assert.Contains(expected, issue!.message);
    }

    [Fact]
    public void VideoOfTheStoryboard_IsCheckedOnce()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events("Video,0,\"A.mp4\"")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFileContents:
            [
                ("MapsetVerifier - MapsetVerifier (Tests).osb", "[Events]\nVideo,0,\"A.mp4\""),
            ],
            extraBinaryFiles: [("A.mp4", TestMedia.Mp4("hvc1"))]
        );

        Assert.Single(context.RunGeneralCheck<CheckVideoCodec>());
    }
}
