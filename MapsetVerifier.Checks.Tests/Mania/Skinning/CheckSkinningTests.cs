using MapsetVerifier.Checks.Mania.Skinning;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Skinning;

public class CheckSkinningTests
{
    private static string BuildMinimalOsu(string hitObject = "256,192,1000,1,0,0:0:0:0:") =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 3",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Test",
            "[Difficulty]",
            "StackLeniency:0.7",
            "CircleSize:4",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            hitObject
        );

    [Fact]
    public void CompleteHitburstSet_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "mania-hit0.png",
                "mania-hit50.png",
                "mania-hit100.png",
                "mania-hit200.png",
                "mania-hit300.png",
                "mania-hit300g.png",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void IncompleteHitburstSet_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "mania-hit0.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Hitburst", issues[0].message);
    }

    [Fact]
    public void StageElements_AreNotBeatmapSkinnable_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "mania-stage-left.png", "mania-stage-right.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void NonPngElement_EmitsWarning()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "mania-hit0.png",
                "mania-hit50.png",
                "mania-hit100.png",
                "mania-hit200.png",
                "mania-hit300.png",
                "mania-hit300g.jpg",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(issues, issue => issue.level == Issue.Level.Warning);
    }
}
