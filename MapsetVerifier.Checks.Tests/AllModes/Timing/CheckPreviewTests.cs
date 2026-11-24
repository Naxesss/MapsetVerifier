using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckPreviewTests
{
    private static List<Issue> Run(params string?[] previewTimes)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            previewTimes.Select(
                (time, i) =>
                {
                    var code = new OsuBuilder()
                        .Version($"Diff {i}")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build();

                    return (
                        $"{i}.osu",
                        time == null ? code : OsuText.General(code, $"PreviewTime: {time}")
                    );
                }
            )
        );

        return context.RunBeatmapSetCheck<CheckPreview>();
    }

    [Fact]
    public void SamePreviewPoint_IsFine()
    {
        Assert.Empty(Run("1000", "1000"));
        Assert.Empty(Run("0"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("-1")]
    public void NotSet_IsAProblem(string? time)
    {
        var issue = Assert.Single(Run(time));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("not set", issue.message);
    }

    [Fact]
    public void Inconsistent_IsAProblem_AgainstTheFirstDifficulty()
    {
        var issue = Assert.Single(Run("1000", "1000", "1001"));

        Assert.Equal("Diff 2", issue.beatmap!.MetadataSettings.version);
        Assert.Contains("inconsistent, see [Diff 0]", issue.message);
    }
}
