using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckGenreLanguageTests
{
    [Theory]
    [InlineData("rock english", false, false)]
    [InlineData("ROCK English", false, false)]
    [InlineData("video game instrumental", false, false)]
    // Both words are needed.
    [InlineData("video instrumental", true, false)]
    [InlineData("rock", false, true)]
    [InlineData("japanese", true, true, false)]
    [InlineData("", true, true)]
    public void MissingTags_AreWarnedAbout(
        string tags,
        bool missesGenre,
        bool missesLanguage,
        bool strict = true
    )
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Tags(tags).WithDefaultTiming().WithDefaultHitObject()
        );

        var issues = context.RunGeneralCheck<CheckGenreLanguage>();

        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
        Assert.Equal(missesGenre, issues.Any(issue => issue.message.Contains("genre")));
        if (strict)
            Assert.Equal(missesLanguage, issues.Any(issue => issue.message.Contains("language")));
        else
            Assert.DoesNotContain(issues, issue => issue.message.Contains("language"));
    }
}
