using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Checks.Tests;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckUnusedFilesTests
{
    private static string BuildMinimalOsu(
        int countdown = 0,
        Beatmap.Mode mode = Beatmap.Mode.Standard
    ) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: " + (int)mode,
            "Countdown: " + countdown,
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Test",
            "[Difficulty]",
            "StackLeniency:0.7",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            "256,192,1000,1,0,0:0:0:0:"
        );

    [Fact]
    public void LazerOnlySkinFile_EmitsInfoNotProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "fountain-shoot.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Info);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("fountain-shoot.wav", issues[0].message);
    }

    [Fact]
    public void TrulyUnusedFile_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "random-unused.bin"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Info);
    }

    [Fact]
    public void CountWav_WithCountdown_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(countdown: 1))],
            ["audio.mp3", "count.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Empty(issues);
    }

    [Fact]
    public void ApplauseWav_StableSkin_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "applause.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Empty(issues);
    }

    [Fact]
    public void LazerSliderMissOnManiaOnlySet_EmitsProblemNotInfo()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(mode: Beatmap.Mode.Mania))],
            ["audio.mp3", "sliderendmiss.png"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Info);
    }
}
