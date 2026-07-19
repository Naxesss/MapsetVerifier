using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckGuestTagsTests
{
    [Theory]
    [InlineData("Take's Hard", "take")]
    [InlineData("(Take's Hard)", "take")]
    [InlineData("lu^3's Extra", "lu^3")]
    [InlineData("[[[[[['s Easy feat. jjen", "[[[[[[")]
    [InlineData("Naxess' Insane", "naxess")]
    [InlineData("Someone else's Normal", "someone else")]
    public void FlagsMissingGuestTag(string version, string expectedTag)
    {
        using var context = CreateContext(version);

        var issues = context.RunGeneralCheck<CheckGuestTags>();

        var warning = Assert.Single(issues, issue => issue.level == Issue.Level.Warning);
        Assert.Contains($"\"{expectedTag}\"", warning.message);
    }

    [Theory]
    [InlineData("Take's Hard", "take")]
    [InlineData("(Take's Hard)", "take")]
    [InlineData("lu^3's Extra", "lu^3")]
    [InlineData("[[[[[['s Easy feat. jjen", "[[[[[[")]
    public void DoesNotFlagWhenGuestTagIsPresent(string version, string tag)
    {
        using var context = CreateContext(version, tag);

        var issues = context.RunGeneralCheck<CheckGuestTags>();

        Assert.Empty(issues);
    }

    private static CheckTestContext CreateContext(string version, string tags = "") =>
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
                    "Title:Song",
                    "Artist:Tests",
                    "Creator:Host",
                    $"Version:{version}",
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
