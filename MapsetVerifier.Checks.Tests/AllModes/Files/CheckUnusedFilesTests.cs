using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckUnusedFilesTests
{
    private static string BuildMinimalOsu(
        int countdown = 0,
        Beatmap.Mode mode = Beatmap.Mode.Standard,
        IEnumerable<string>? events = null,
        string hitObject = "256,192,1000,1,0,0:0:0:0:"
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
            string.Join("\n", events ?? []),
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            hitObject
        );

    private const string ExpectedOsbFileName = "Artist - Title (Creator).osb";

    private static void AssertNoUnusedIssues(CheckTestContext context)
    {
        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Empty(issues);
    }

    private static void AssertProblemFor(CheckTestContext context, string filePath)
    {
        var normalizedFilePath = filePath.Replace("\\", "/");
        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains(
            issues,
            issue => issue.message.Replace("\\", "/").Contains(normalizedFilePath)
        );
    }

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
    public void AudioFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExactStoryboardReferenceInOsu_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    BuildMinimalOsu(events: ["Sprite,Foreground,Centre,\"SB\\white.png\",320,200"])
                ),
            ],
            ["audio.mp3", "SB/white.png"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExactStoryboardReferenceInOsb_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "SB/white.png"],
            [(ExpectedOsbFileName, "[Events]\nSprite,Foreground,Centre,\"SB\\white.png\",320,200")]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExtensionlessStoryboardReference_UsesMatchingFile()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    BuildMinimalOsu(events: ["Sprite,Foreground,Centre,\"SB\\white\",320,200"])
                ),
            ],
            ["audio.mp3", "SB/white.png"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void StoryboardReferenceWithDifferentExtension_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "SB/white.png"],
            [
                (
                    ExpectedOsbFileName,
                    string.Join(
                        "\n",
                        "[Events]",
                        "Sprite,Foreground,Centre,\"SB\\white.jpg\",320,200",
                        " L,3175914,1",
                        "  MY,0,0,1000,200,240",
                        "  V,0,0,1000,1,0.75,1,1.8",
                        "  F,0,1000,,0"
                    )
                ),
            ]
        );

        AssertProblemFor(context, "SB/white.png");
    }

    [Fact]
    public void HitObjectCustomSampleFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(hitObject: "256,192,1000,1,0,0:0:0:0:custom.wav"))],
            ["audio.mp3", "custom.wav"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void GeneratedHitSoundFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(hitObject: "256,192,1000,1,2,0:0:1:0:"))],
            ["audio.mp3", "soft-hitwhistle.wav"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void UsedOsbFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3"],
            [
                (
                    ExpectedOsbFileName,
                    "[Events]\nSprite,Foreground,Centre,\"SB\\missing.png\",320,200"
                ),
            ]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void EmptyOsbFile_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", ExpectedOsbFileName]
        );

        AssertProblemFor(context, ExpectedOsbFileName);
    }

    [Fact]
    public void AnimationFrameFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    BuildMinimalOsu(
                        events:
                        [
                            "Animation,Foreground,Centre,\"SB\\frame.png\",320,200,2,100,LoopOnce",
                        ]
                    )
                ),
            ],
            ["audio.mp3", "SB/frame0.png", "SB/frame1.png"]
        );

        AssertNoUnusedIssues(context);
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
