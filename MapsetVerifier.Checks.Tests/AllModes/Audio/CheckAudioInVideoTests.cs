using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckAudioInVideoTests
{
    private static Issue Run(string path, byte[]? video)
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

        return Assert.Single(context.RunGeneralCheck<CheckAudioInVideo>());
    }

    [Fact]
    public void MissingVideo_IsAWarning()
    {
        var issue = Run("a.mp4", null);

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("downloaded with video", issue.message);
    }

    [Fact]
    public void OutsideOfTheFolder_IsAProblem()
    {
        Assert.Equal(Issue.Level.Problem, Run("../a.mp4", null).level);
    }

    [Fact]
    public void UnreadableVideo_CouldNotBeChecked()
    {
        Assert.Equal(Issue.Level.Error, Run("a.xyz", [1, 2, 3]).level);
    }

    [Fact]
    public void VideoWithoutAudio_IsFine()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("test.osu", new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Build()),
        ]);

        Assert.Empty(context.RunGeneralCheck<CheckAudioInVideo>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VideoWithSound_IsAProblem(bool sound)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events("Video,0,\"a.avi\"")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: [("a.avi", TestMedia.Avi(sound))]
        );

        var issues = context.RunGeneralCheck<CheckAudioInVideo>();

        if (!sound)
        {
            Assert.Empty(issues);
            return;
        }

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("a.avi", issue.message);
    }

    [Fact]
    public void SoundAlone_IsNotAVideo()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events("Video,0,\"a.mp3\"")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: [("a.mp3", TestMedia.Mp3())]
        );

        Assert.Empty(context.RunGeneralCheck<CheckAudioInVideo>());
    }
}
