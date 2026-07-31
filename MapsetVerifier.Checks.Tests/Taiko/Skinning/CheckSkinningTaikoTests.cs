using MapsetVerifier.Checks.Taiko.Skinning;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Skinning;

public class CheckSkinningTaikoTests
{
    private static string BuildMinimalOsu(string? hitObject = null) =>
        new OsuBuilder()
            .Mode(Beatmap.Mode.Taiko)
            .Title("Title")
            .Artist("Artist")
            .Creator("Creator")
            .StackLeniency(0.7f)
            .WithDefaultTiming()
            .HitObjects(hitObject ?? OsuBuilder.DefaultHitObject)
            .Build();

    private static string BuildMinimalStandardOsu() =>
        new OsuBuilder()
            .Title("Title")
            .Artist("Artist")
            .Creator("Creator")
            .Version("StandardDiff")
            .StackLeniency(0.7f)
            .WithDefaultTiming()
            .WithDefaultHitObject()
            .Build();

    private const string DrumrollHitObject = "256,192,1000,2,0,B|300:200,1,50,0|0,0:0|0:0,0:0:0:0:";

    private static readonly string[] BaseHitObjectElements =
    [
        "taikobigcircle.png",
        "taikobigcircleoverlay.png",
        "taikohitcircle.png",
        "taikohitcircleoverlay.png",
        "taiko-roll-middle.png",
        "taiko-roll-end.png",
    ];

    [Fact]
    public void CompleteHitburstSet_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "taiko-hit0.png",
                "taiko-hit100.png",
                "taiko-hit100k.png",
                "taiko-hit300.png",
                "taiko-hit300k.png",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Empty(issues);
    }

    [Fact]
    public void IncompleteHitburstSet_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "taiko-hit0.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Hitburst", issues[0].message);
    }

    [Fact]
    public void DrumrollElementsNotRequired_WithoutDrumrolls()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "taikobigcircle.png",
                "taikobigcircleoverlay.png",
                "taikohitcircle.png",
                "taikohitcircleoverlay.png",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Empty(issues);
    }

    [Fact]
    public void DrumrollElementsIncomplete_WithDrumrolls_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(DrumrollHitObject))],
            ["audio.mp3", "taiko-roll-middle.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Hit object", issues[0].message);
    }

    [Fact]
    public void NonPngElement_EmitsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "taiko-hit0.png",
                "taiko-hit100.png",
                "taiko-hit100k.png",
                "taiko-hit300.png",
                "taiko-hit300k.jpg",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Contains(issues, issue => issue.level == Issue.Level.Warning);
    }

    [Fact]
    public void SliderScorepointNotRequired_WhenOsuDifficultyExists()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("taiko.osu", BuildMinimalOsu(DrumrollHitObject)),
                ("standard.osu", BuildMinimalStandardOsu()),
            ],
            ["audio.mp3", .. BaseHitObjectElements]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Empty(issues);
    }

    [Fact]
    public void SliderScorepointRequired_WithoutOsuDifficulty()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("taiko.osu", BuildMinimalOsu(DrumrollHitObject))],
            ["audio.mp3", .. BaseHitObjectElements]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Hit object")
                && issue.message.Contains("sliderscorepoint.png")
        );
    }

    [Fact]
    public void SharedSliderScorepointAlone_WithOsuDifficulty_DoesNotTriggerIncompleteSet()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                ("taiko.osu", BuildMinimalOsu(DrumrollHitObject)),
                ("standard.osu", BuildMinimalStandardOsu()),
            ],
            ["audio.mp3", "sliderscorepoint.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningTaiko>();

        Assert.Empty(issues);
    }
}
