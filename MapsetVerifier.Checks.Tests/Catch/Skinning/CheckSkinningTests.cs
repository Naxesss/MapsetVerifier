using MapsetVerifier.Checks.Catch.Skinning;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Skinning;

public class CheckSkinningTests
{
    private static string BuildMinimalOsu(string hitObject = "256,192,1000,1,0,0:0:0:0:") =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 2",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Test",
            "[Difficulty]",
            "StackLeniency:0.7",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            hitObject
        );

    private const string DropletHitObject = "256,192,1000,2,0,B|300:200,1,50,0|0,0:0|0:0,0:0:0:0:";

    private const string SpinnerHitObject = "256,192,1000,8,0,2000,0:0:0:0:";

    private const string SkinIniV2 = "[General]\nVersion: 2";

    [Fact]
    public void CompleteCatcherSet_WithV2SkinIni_NoIssues()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "fruit-catcher-fail.png",
                "fruit-catcher-idle.png",
                "fruit-catcher-kiai.png",
            ],
            [("skin.ini", SkinIniV2)]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void IncompleteCatcherSet_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            ["audio.mp3", "fruit-catcher-fail.png"],
            [("skin.ini", SkinIniV2)]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Catcher", issues[0].message);
    }

    [Fact]
    public void CustomCatcher_WithoutSkinIni_EmitsOldSkinVersionProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "fruit-catcher-fail.png",
                "fruit-catcher-idle.png",
                "fruit-catcher-kiai.png",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("skin.ini")
        );
    }

    [Fact]
    public void CustomCatcher_WithV1SkinIni_EmitsOldSkinVersionProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "fruit-catcher-fail.png",
                "fruit-catcher-idle.png",
                "fruit-catcher-kiai.png",
            ],
            [("skin.ini", "[General]\nVersion: 1")]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("skin.ini")
        );
    }

    [Fact]
    public void DropFruitElementsNotRequired_WithoutDroplets()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu())],
            [
                "audio.mp3",
                "fruit-apple.png",
                "fruit-apple-overlay.png",
                "fruit-grapes.png",
                "fruit-grapes-overlay.png",
                "fruit-orange.png",
                "fruit-orange-overlay.png",
                "fruit-pear.png",
                "fruit-pear-overlay.png",
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Empty(issues);
    }

    [Fact]
    public void DropFruitElements_IncompleteWithDroplets_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(DropletHitObject))],
            ["audio.mp3", "fruit-drop.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Fruits", issues[0].message);
    }

    [Fact]
    public void BananaElements_IncompleteWithSpinner_EmitsProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", BuildMinimalOsu(SpinnerHitObject))],
            ["audio.mp3", "fruit-bananas.png"]
        );

        var issues = context.RunBeatmapSetCheck<CheckSkinning>();

        Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("Fruits", issues[0].message);
    }

    [Fact]
    public void FruitWithCorrectDimensions_NoIssues()
    {
        var tempPath = CreateMapsetWithImage("fruit-apple.png", 128, 128);

        try
        {
            var beatmapSet = new BeatmapSet(tempPath);
            var issues = new CheckSkinning().GetIssues(beatmapSet).ToList();

            Assert.DoesNotContain(issues, issue => issue.message.Contains("Dimensions"));
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Fact]
    public void FruitWithIncorrectDimensions_EmitsProblem()
    {
        var tempPath = CreateMapsetWithImage("fruit-apple.png", 64, 64);

        try
        {
            var beatmapSet = new BeatmapSet(tempPath);
            var issues = new CheckSkinning().GetIssues(beatmapSet).ToList();

            Assert.Contains(
                issues,
                issue =>
                    issue.level == Issue.Level.Problem
                    && issue.message.Contains("fruit-apple.png")
                    && issue.message.Contains("128x128")
            );
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    private static string CreateMapsetWithImage(string imageFileName, int width, int height)
    {
        var tempPath = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierChecksTests",
            "Generated",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(tempPath);

        File.WriteAllText(Path.Combine(tempPath, "test.osu"), BuildMinimalOsu());
        File.WriteAllText(Path.Combine(tempPath, "audio.mp3"), string.Empty);

        using var image = new Image<Rgba32>(width, height);
        image.SaveAsPng(Path.Combine(tempPath, imageFileName));

        return tempPath;
    }
}
