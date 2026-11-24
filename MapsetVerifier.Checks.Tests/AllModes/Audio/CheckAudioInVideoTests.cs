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
}
