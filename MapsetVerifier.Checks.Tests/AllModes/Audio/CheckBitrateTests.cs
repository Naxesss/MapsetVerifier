using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckBitrateTests
{
    private static List<Issue> Run(byte[] audio)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .AudioFilename("audio.wav")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraBinaryFiles: [("audio.wav", audio)]
        );

        return context.RunGeneralCheck<CheckBitrate>();
    }

    [Fact]
    public void HighBitrate_IsAProblem()
    {
        // 44.1 kHz at 16 bit stereo is 1411 kbps.
        var issue = Assert.Single(
            Run(TestMedia.Wave(500, TestMedia.Level(0.5), TestMedia.Level(0.5)))
        );

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("1411 kbps, is too high", issue.message);
    }

    [Fact]
    public void BrokenAudio_CouldNotBeChecked()
    {
        Assert.Equal(Issue.Level.Error, Assert.Single(Run([1, 2, 3])).level);
    }

    [Fact]
    public void MissingAudio_IsLeftToAnotherCheck()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().WithDefaultTiming().WithDefaultHitObject()
        );

        Assert.Empty(context.RunGeneralCheck<CheckBitrate>());
    }

    [Theory]
    [InlineData(9, null)] // 128 kbps
    [InlineData(10, null)] // 160 kbps
    [InlineData(5, "64 kbps, is too low")]
    [InlineData(14, "kbps, is too high")]
    public void Mp3Bitrate(byte index, string? expected)
    {
        var issue = Run(TestMedia.Mp3(bitrateIndex: index)).SingleOrDefault();

        if (expected == null)
            Assert.Null(issue);
        else
            Assert.Contains(expected, issue!.message);
    }

    [Fact]
    public void BitrateJustAtTheLowerLimit_IsFine()
    {
        Assert.Empty(Run(TestMedia.Mp3(400)));
    }

    [Fact]
    public void Ogg_MayHaveAHigherBitrate()
    {
        Assert.Empty(Run(TestMedia.Ogg(199)));
        Assert.Contains("too high", Assert.Single(Run(TestMedia.Ogg(223))).message);
    }
}
