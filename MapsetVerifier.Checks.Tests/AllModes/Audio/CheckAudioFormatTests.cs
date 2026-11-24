using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckAudioFormatTests
{
    private static List<Issue> Run(byte[]? audio)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Build())],
            extraBinaryFiles: audio == null ? null : [("audio.mp3", audio)]
        );

        return context.RunGeneralCheck<CheckAudioFormat>();
    }

    [Fact]
    public void WaveNamedMp3_IsAProblem()
    {
        var issue = Assert.Single(Run(TestMedia.Wave(100, TestMedia.Level(0.5))));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("\"audio.mp3\" is using the", issue.message);
    }

    [Fact]
    public void BrokenAudio_CouldNotBeChecked()
    {
        Assert.Equal(Issue.Level.Error, Assert.Single(Run([1, 2, 3])).level);
    }

    [Fact]
    public void MissingAudio_IsLeftToAnotherCheck()
    {
        Assert.Empty(Run(null));
    }

    [Theory]
    [InlineData("audio.mp3", null)]
    [InlineData("audio.ogg", Issue.Level.Warning)]
    public void Mp3_IsFine_IfNamedAsSuch(string name, Issue.Level? expected)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .AudioFilename(name)
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: [(name, TestMedia.Mp3())]
        );

        Assert.Equal(
            expected,
            context.RunGeneralCheck<CheckAudioFormat>().SingleOrDefault()?.level
        );
    }
}
