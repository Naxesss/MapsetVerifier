using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckUnusedFilesTests
{
    private static OsuBuilder BuildMinimalOsu(
        int countdown = 0,
        Beatmap.Mode mode = Beatmap.Mode.Standard,
        IEnumerable<string>? events = null,
        string hitObject = "256,192,1000,1,0,0:0:0:0:"
    ) =>
        new OsuBuilder()
            .Mode(mode)
            .Countdown(countdown)
            .Title("Title")
            .Artist("Artist")
            .Creator("Creator")
            .StackLeniency(0.7f)
            .Events(events ?? [])
            .WithDefaultTiming()
            .HitObjects(hitObject);

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
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", "fountain-shoot.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Info);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("fountain-shoot.wav", issues[0].message);
    }

    [Fact]
    public void TrulyUnusedFile_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", "random-unused.bin"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Info);
    }

    [Fact]
    public void AudioFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExactStoryboardReferenceInOsu_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(events: ["Sprite,Foreground,Centre,\"SB\\white.png\",320,200"]),
            extraFiles: ["audio.mp3", "SB/white.png"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExactStoryboardReferenceInOsb_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", "SB/white.png"],
            extraFileContents:
            [
                (
                    ExpectedOsbFileName,
                    "[Events]\nSprite,Foreground,Centre,\"SB\\white.png\",320,200"
                ),
            ]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void ExtensionlessStoryboardReference_UsesMatchingFile()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(events: ["Sprite,Foreground,Centre,\"SB\\white\",320,200"]),
            extraFiles: ["audio.mp3", "SB/white.png"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void StoryboardReferenceWithDifferentExtension_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", "SB/white.png"],
            extraFileContents:
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
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(hitObject: "256,192,1000,1,0,0:0:0:0:custom.wav"),
            extraFiles: ["audio.mp3", "custom.wav"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void GeneratedHitSoundFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(hitObject: "256,192,1000,1,2,0:0:1:0:"),
            extraFiles: ["audio.mp3", "soft-hitwhistle.wav"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void UsedOsbFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3"],
            extraFileContents:
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
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", ExpectedOsbFileName]
        );

        AssertProblemFor(context, ExpectedOsbFileName);
    }

    [Fact]
    public void AnimationFrameFile_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(
                events: ["Animation,Foreground,Centre,\"SB\\frame.png\",320,200,2,100,LoopOnce"]
            ),
            extraFiles: ["audio.mp3", "SB/frame0.png", "SB/frame1.png"]
        );

        AssertNoUnusedIssues(context);
    }

    [Fact]
    public void CountWav_WithCountdown_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(countdown: 1),
            extraFiles: ["audio.mp3", "count.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Empty(issues);
    }

    [Fact]
    public void ApplauseWav_StableSkin_NoUnusedIssue()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(),
            extraFiles: ["audio.mp3", "applause.wav"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Empty(issues);
    }

    [Fact]
    public void LazerSliderMissOnManiaOnlySet_EmitsProblemNotInfo()
    {
        using var context = CheckTestContext.CreateFromOsu(
            BuildMinimalOsu(mode: Beatmap.Mode.Mania),
            extraFiles: ["audio.mp3", "sliderendmiss.png"]
        );

        var issues = context.RunGeneralCheck<CheckUnusedFiles>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Info);
    }
}
