using MapsetVerifier.Checks.Mania.Settings;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Settings;

public class CheckPlaystyleTagsTests
{
    [Theory]
    [InlineData(4, "Hard", "")]
    [InlineData(8, "[8K] Hard", "")]
    [InlineData(8, "[7K+1] Hard", "")]
    [InlineData(10, "Hard", "")]
    [InlineData(12, "[10K2S] Hard", "jazz 10k2s")]
    [InlineData(16, "[DP] Hard", "dp")]
    [InlineData(18, "[10K8K] Hard", "4k10k4k")]
    // The name doesn't say which one it is, so either is fine.
    [InlineData(14, "[14K] Hard", "ez2ac")]
    // Not an allowed key count to begin with.
    [InlineData(13, "Hard", "")]
    public void NoIssue(int keys, string version, string tags) =>
        Assert.Empty(Run(keys, version, tags));

    [Theory]
    [InlineData(12, "[10K2S] Hard", "jazz", "10K2S")]
    [InlineData(16, "[DP] Hard", "ez2ac", "DP")]
    // Part of another word isn't the tag.
    [InlineData(16, "[DP] Hard", "mdpi", "DP")]
    [InlineData(18, "[18K] Hard", "", "10K8K or 9K9K")]
    public void Missing(int keys, string version, string tags, string playstyle) =>
        Assert.Contains(
            $"of this difficulty, {playstyle}.",
            Assert.Single(Run(keys, version, tags))
        );

    private static List<string> Run(int keys, string version, string tags)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .CircleSize(keys)
                .Version(version)
                .Tags(tags)
                .WithDefaultTiming()
                .WithDefaultHitObject()
        );

        return context
            .RunBeatmapCheck<CheckPlaystyleTags>(version)
            .Select(issue => issue.message)
            .ToList();
    }
}
