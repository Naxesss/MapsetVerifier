using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
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

    [Theory]
    [InlineData("6K Enma's Easy", 6)]
    [InlineData("[7K] Enma's Hard", 7)]
    [InlineData("Enma's 4K Insane", 4)]
    [InlineData("Enma's [7K] - Insane", 7)]
    [InlineData("8K (7+1) Enma's Normal", 8)]
    public void IgnoresManiaKeyModeMarkerInGuestName(string version, int keys)
    {
        using var flagged = CreateManiaContext(version, keys);
        var warning = Assert.Single(flagged.RunGeneralCheck<CheckGuestTags>());
        Assert.Contains("\"enma\"", warning.message);

        using var tagged = CreateManiaContext(version, keys, "enma");
        Assert.Empty(tagged.RunGeneralCheck<CheckGuestTags>());
    }

    [Fact]
    public void KeepsKeyModeLikeTextOutsideMania()
    {
        using var context = CreateContext("6K Enma's Easy");

        var warning = Assert.Single(context.RunGeneralCheck<CheckGuestTags>());

        Assert.Contains("\"6k enma\"", warning.message);
    }

    private static CheckTestContext CreateContext(string version, string tags = "") =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title("Song").Creator("Host").Version(version).Tags(tags)
        );

    private static CheckTestContext CreateManiaContext(
        string version,
        int keys,
        string tags = ""
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .CircleSize(keys)
                .Title("Song")
                .Creator("Host")
                .Version(version)
                .Tags(tags)
        );
}
