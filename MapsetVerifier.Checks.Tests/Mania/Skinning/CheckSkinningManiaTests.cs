using MapsetVerifier.Checks.Mania.Skinning;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Skinning;

public class CheckSkinningManiaTests
{
    private static string BuildMinimalOsu(string hitObject = OsuBuilder.DefaultHitObject) =>
        new OsuBuilder()
            .Mode(Beatmap.Mode.Mania)
            .Title("Title")
            .Artist("Artist")
            .Creator("Creator")
            .StackLeniency(0.7f)
            .WithDefaultTiming()
            .HitObjects(hitObject)
            .Build();

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

        var issues = context.RunBeatmapSetCheck<CheckSkinningMania>();

        Assert.Empty(issues);
    }

    [Fact]
    public void IncompleteHitburstSet_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "mania-hit0.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinningMania>();

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

        var issues = context.RunBeatmapSetCheck<CheckSkinningMania>();

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

        var issues = context.RunBeatmapSetCheck<CheckSkinningMania>();

        Assert.Contains(issues, issue => issue.level == Issue.Level.Warning);
    }
}
