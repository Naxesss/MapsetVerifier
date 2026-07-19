using MapsetVerifier.Checks.AllModes.General.Resources;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckMappersGuildBackgroundTests
{
    [Theory]
    [InlineData("mpg")]
    [InlineData("mappers guild")]
    [InlineData("mappers' guild")]
    [InlineData("mappersguild")]
    [InlineData("MPG")]
    [InlineData("something mappers' guild mpg fa featured artist")]
    public void MappersGuildTag_FlagsInfoIssue(string tags)
    {
        using var context = CreateContext(tags);

        var issues = context.RunGeneralCheck<CheckMappersGuildBackground>();

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Info, issue.level);
        Assert.Contains("free to use", issue.message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("electronic instrumental greaper very cool song tags")]
    public void NoMappersGuildTag_ProducesNoIssues(string tags)
    {
        using var context = CreateContext(tags);

        var issues = context.RunGeneralCheck<CheckMappersGuildBackground>();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(string tags) =>
        CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                string.Join(
                    "\n",
                    "osu file format v14",
                    "[General]",
                    "AudioFilename:audio.mp3",
                    "Mode: 0",
                    "[Metadata]",
                    "Title:Test",
                    "Artist:Tests",
                    "Creator:Tests",
                    "Version:Test",
                    $"Tags:{tags}",
                    "[Difficulty]",
                    "CircleSize:4",
                    "HPDrainRate:5",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[Events]",
                    "[TimingPoints]",
                    "[HitObjects]"
                )
            ),
        ]);
}
