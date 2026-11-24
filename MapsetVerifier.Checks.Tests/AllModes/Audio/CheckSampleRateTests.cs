using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckSampleRateTests
{
    private static List<Issue> Run(byte[]? audio)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Build())],
            extraBinaryFiles: audio == null ? null : [("audio.mp3", audio)]
        );

        return context.RunGeneralCheck<CheckSampleRate>();
    }

    private static byte[] Wave(int sampleRate)
    {
        var wave = TestMedia.Wave(100, TestMedia.Level(0.5));
        BitConverter.GetBytes(sampleRate).CopyTo(wave, 24);
        BitConverter.GetBytes(sampleRate * 2).CopyTo(wave, 28);

        return wave;
    }

    [Theory]
    [InlineData(44100, null)]
    [InlineData(48000, null)]
    [InlineData(96000, "96 kHz")]
    public void Above48Khz_IsAProblem(int sampleRate, string? expected)
    {
        var issues = Run(Wave(sampleRate));

        Assert.Equal(expected != null, issues.Count == 1);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
        Assert.All(issues, issue => Assert.Contains(expected!, issue.message));
    }

    [Fact]
    public void BrokenAudio_CouldNotBeChecked_AndMissingIsLeftToAnotherCheck()
    {
        Assert.Equal(Issue.Level.Error, Assert.Single(Run([9, 9, 9])).level);
        Assert.Empty(Run(null));
    }
}
