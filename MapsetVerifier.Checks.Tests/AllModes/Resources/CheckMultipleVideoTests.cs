using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckMultipleVideoTests
{
    private static List<Issue> Run(
        (Beatmap.Mode Mode, string? Video)[] difficulties,
        string? osbVideo = null
    )
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            difficulties.Select(
                (difficulty, i) =>
                    (
                        $"{i}.osu",
                        new OsuBuilder()
                            .Artist("Artist")
                            .Title("Title")
                            .Creator("Creator")
                            .Mode(difficulty.Mode)
                            .Version($"Diff {i}")
                            .Events(
                                difficulty.Video == null ? [] : [$"Video,0,\"{difficulty.Video}\""]
                            )
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    )
            ),
            extraFileContents: osbVideo == null
                ? null
                : [("Artist - Title (Creator).osb", $"[Events]\nVideo,0,\"{osbVideo}\"")]
        );

        return context.RunGeneralCheck<CheckMultipleVideo>();
    }

    [Fact]
    public void SameOrNoVideo_IsFine()
    {
        Assert.Empty(Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Standard, "a.mp4")]));
        Assert.Empty(Run([(Beatmap.Mode.Standard, null), (Beatmap.Mode.Standard, null)]));
    }

    [Fact]
    public void DifferentWithinAMode_ListsTheDifficultiesOfEach()
    {
        var issues = Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Standard, null)]);

        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
        Assert.Equal(
            ["None : [Diff 1].", "a.mp4 : [Diff 0]."],
            issues.Select(issue => issue.message).Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void DifferentBetweenModes_IsMentionedOnce_ExceptForTaiko()
    {
        var issue = Assert.Single(
            Run([
                (Beatmap.Mode.Standard, "a.mp4"),
                (Beatmap.Mode.Taiko, null),
                (Beatmap.Mode.Mania, null),
            ])
        );

        Assert.Equal("Inconsistent video between the standard and mania beatmaps.", issue.message);
    }

    [Fact]
    public void StoryboardVideo_StandsInForDifficultiesWithout()
    {
        Assert.Empty(
            Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Standard, null)], "a.mp4")
        );
        Assert.NotEmpty(Run([(Beatmap.Mode.Standard, "a.mp4")], "b.mp4"));
    }

    [Fact]
    public void VideoInTheStoryboard_CountsForEveryDifficulty()
    {
        Assert.NotEmpty(
            Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Standard, null)], "b.mp4")
        );
        Assert.NotEmpty(
            Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Catch, "b.mp4")], "c.mp4")
        );
    }

    [Fact]
    public void DifferentBetweenModes_IsMentionedOncePerPair_ExceptTaiko()
    {
        Assert.Single(Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Catch, "b.mp4")]));
        Assert.Empty(Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Taiko, null)]));
        Assert.Empty(Run([(Beatmap.Mode.Taiko, null), (Beatmap.Mode.Standard, "a.mp4")]));
        Assert.Equal(
            3,
            Run([
                (Beatmap.Mode.Standard, "a.mp4"),
                (Beatmap.Mode.Catch, "b.mp4"),
                (Beatmap.Mode.Mania, "c.mp4"),
            ]).Count
        );
    }

    [Fact]
    public void Taiko_MayDiffer()
    {
        Assert.Empty(Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Taiko, null)]));
        Assert.Empty(Run([(Beatmap.Mode.Taiko, null), (Beatmap.Mode.Standard, "a.mp4")]));
        Assert.NotEmpty(Run([(Beatmap.Mode.Standard, "a.mp4"), (Beatmap.Mode.Catch, null)]));
    }
}
