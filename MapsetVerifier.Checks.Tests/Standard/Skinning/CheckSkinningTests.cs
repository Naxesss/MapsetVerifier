using MapsetVerifier.Checks.Standard.Skinning;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Skinning;

public class CheckSkinningTests
{
    private static string BuildMinimalOsu(
        string hitObject = "256,192,1000,1,0,0:0:0:0:",
        string? colours = null
    ) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Test",
            "[Difficulty]",
            "StackLeniency:0.7",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            colours == null ? "" : "[Colours]\n" + colours,
            "[HitObjects]",
            hitObject
        );

    private const string CircleHitObject = "256,192,1000,1,0,0:0:0:0:";

    private const string SliderHitObject = "256,192,1000,2,0,B|300:200,1,50,0|0,0:0|0:0,0:0:0:0:";

    [Fact]
    public void CompleteCursorSet_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "cursor.png", "cursortrail.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void IncompleteCursorSet_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "cursor.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Cursor", issues[0].message);
        Assert.Contains("cursortrail.png", issues[0].message);
    }

    [Fact]
    public void NonPngElement_EmitsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "cursor.png", "cursortrail.jpg"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(issues, issue => issue.level == Issue.Level.Warning);
        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void SlidertrackSet_NotSkinnedWithoutSliders_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(CircleHitObject))],
            ["audio.mp3", "sliderb0.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void SlidertrackSet_IncompleteWithSliders_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(SliderHitObject))],
            ["audio.mp3", "sliderb0.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("Slidertrack")
        );
    }

    [Fact]
    public void HitburstSet_AnimatedFramesSatisfyRequirement()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "hit0-0.png", "hit0-1.png", "hit50.png"],
            [
                ("hit100.png", "hit100-content"),
                ("hit100k.png", "hit100k-content"),
                ("hit300.png", "hit300-content"),
                ("hit300g.png", "hit300g-content"),
                ("hit300k.png", "hit300k-content"),
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void MixedSpinnerStyles_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu("256,192,1000,8,0,2000,0:0:0:0:"))],
            ["audio.mp3", "spinner-circle.png", "spinner-bottom.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("spinner-circle.png")
                && issue.message.Contains("spinner-bottom.png")
        );
    }

    [Fact]
    public void DuplicateHit100Hitburst_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3"],
            [("hit100.png", "same-content"), ("hit100k.png", "same-content")]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("hit100.png")
                && issue.message.Contains("hit100k.png")
        );
    }

    [Fact]
    public void HitcircleSkinned_WithoutSliderBorderColour_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "hitcircle.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("SliderBorder")
        );
    }

    [Fact]
    public void HitcircleSkinned_WithSliderBorderColour_NoSliderBorderProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(colours: "SliderBorder : 255,255,255"))],
            ["audio.mp3", "hitcircle.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.DoesNotContain(issues, issue => issue.message.Contains("SliderBorder"));
    }

    [Fact]
    public void SimilarSliderColours_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    BuildMinimalOsu(
                        colours: "SliderBorder : 200,200,200\nSliderTrackOverride : 205,205,205"
                    )
                ),
            ],
            ["audio.mp3", "hitcircle.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("too similar")
        );
    }

    [Fact]
    public void DistinctSliderColours_NoSimilarityProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    BuildMinimalOsu(
                        colours: "SliderBorder : 255,255,255\nSliderTrackOverride : 0,0,0"
                    )
                ),
            ],
            ["audio.mp3", "hitcircle.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.DoesNotContain(issues, issue => issue.message.Contains("too similar"));
    }

    [Fact]
    public void NewSpinnerStyle_WithoutMixing_EmitsWarningNotProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu("256,192,1000,8,0,2000,0:0:0:0:"))],
            ["audio.mp3", "spinner-bottom.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Warning && issue.message.Contains("spinner-bottom.png")
        );
        Assert.DoesNotContain(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("old and new")
        );
    }

    [Fact]
    public void SpinnerOsu_EmitsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu("256,192,1000,8,0,2000,0:0:0:0:"))],
            ["audio.mp3", "spinner-osu.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("spinner-osu.png")
        );
    }
}
