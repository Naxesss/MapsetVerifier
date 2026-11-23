using MapsetVerifier.Checks.Mania.Settings;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Settings;

public class CheckSpecialStyleTests
{
    [Theory]
    // The key count alone doesn't make it N+1.
    [InlineData(8, "Hard", "", false)]
    [InlineData(8, "[8K] Hard", "7k+1", false)]
    [InlineData(8, "[7K+1] Hard", "n+1", true)]
    [InlineData(8, "Hard", "jazz N+1", true)]
    [InlineData(10, "Hard", "", false)]
    // Neither the option nor the tag is asked for above 9 keys.
    [InlineData(10, "[9K+1] Hard", "", false)]
    [InlineData(10, "Hard", "N+1", false)]
    public void NoIssue(int keys, string version, string tags, bool special) =>
        Assert.Empty(Run((keys, version, tags, special)));

    [Theory]
    [InlineData(8, "[7K+1] Hard", "n+1", false, "7K+1, but the special style option is off")]
    [InlineData(8, "[7K+1] Hard", "7k+1", true, "7K+1, but the tags don't include \"N+1\"")]
    [InlineData(10, "Hard", "", true, "on with 10 keys")]
    [InlineData(16, "[DP] Hard", "n+1 dp", true, "on with 16 keys")]
    [InlineData(8, "Hard", "", true, "option is on, but the tags don't include \"N+1\"")]
    [InlineData(8, "Hard", "n+1", false, "no difficulty has the special style option on")]
    public void Issue(int keys, string version, string tags, bool special, string expected) =>
        Assert.Contains(expected, Assert.Single(Run((keys, version, tags, special))));

    [Fact]
    public void NamedN1_WithoutOptionAndTag_IsReportedForBoth() =>
        Assert.Equal(2, Run((8, "[7K+1] Hard", "", false)).Count);

    [Fact]
    public void Tag_NeedsOnlyOneDifficultyWithTheOption() =>
        Assert.Empty(Run((8, "[7K+1] Hard", "n+1", true), (7, "[7K] Hard", "n+1", false)));

    private static List<string> Run(
        params (int Keys, string Version, string Tags, bool Special)[] difficulties
    )
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            difficulties.Select(
                (difficulty, index) =>
                    (
                        $"test{index}.osu",
                        new OsuBuilder()
                            .Mode(Beatmap.Mode.Mania)
                            .CircleSize(difficulty.Keys)
                            .Version(difficulty.Version)
                            .Tags(difficulty.Tags)
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                            .Replace(
                                "Mode: 3",
                                $"Mode: 3\nSpecialStyle: {(difficulty.Special ? 1 : 0)}"
                            )
                    )
            )
        );

        return context
            .RunBeatmapSetCheck<CheckSpecialStyle>()
            .Select(issue => issue.message)
            .ToList();
    }
}
