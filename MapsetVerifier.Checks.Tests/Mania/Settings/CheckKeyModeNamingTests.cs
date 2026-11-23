using MapsetVerifier.Checks.Mania.Settings;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Settings;

public class CheckKeyModeNamingTests
{
    [Fact]
    public void SingleKeyMode_WithoutMarkers_ProducesNoIssues()
    {
        using var context = CreateContext((4, "Easy"), (4, "Hard"));

        Assert.Empty(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
    }

    [Fact]
    public void SingleKeyMode_WithMarker_IsUnneeded()
    {
        using var context = CreateContext((4, "[4K] Easy"), (4, "Hard"));

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
        Assert.Contains(
            "\"[4K] Easy\" shows its key mode, [4K], while every difficulty is 4K",
            issue.message
        );
    }

    [Fact]
    public void MultipleKeyModes_WithMarkers_ProducesNoIssues()
    {
        using var context = CreateContext((4, "[4K] Hard"), (7, "[7K] Hard"));

        Assert.Empty(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
    }

    [Fact]
    public void MultipleKeyModes_MissingMarker_IsReported()
    {
        using var context = CreateContext((4, "[4K] Hard"), (7, "Insane"));

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
        Assert.Contains("\"Insane\" should show its key mode, 7K", issue.message);
    }

    [Fact]
    public void WrongMarker_IsReported()
    {
        using var context = CreateContext((4, "[7K] Hard"), (7, "[7K] Insane"));

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
        Assert.Contains("shows [7K], but is played in 4K", issue.message);
    }

    [Fact]
    public void KeyCountAboveTen_RequiresMarker()
    {
        using var context = CreateContext((12, "Hard"));

        var issue = Assert.Single(context.RunBeatmapSetCheck<CheckKeyModeNaming>());
        Assert.Contains("key count above 10", issue.message);
    }

    private static CheckTestContext CreateContext(
        params (int Keys, string Version)[] difficulties
    ) =>
        CheckTestContext.CreateFromOsuFiles(
            difficulties.Select(
                (difficulty, index) =>
                    (
                        $"test{index}.osu",
                        new OsuBuilder()
                            .Mode(Beatmap.Mode.Mania)
                            .CircleSize(difficulty.Keys)
                            .Version(difficulty.Version)
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    )
            )
        );
}
