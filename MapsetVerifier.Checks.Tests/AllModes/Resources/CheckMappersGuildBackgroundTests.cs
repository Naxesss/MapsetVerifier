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
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title("Test").Artist("Tests").Creator("Tests").Tags(tags)
        );
}
