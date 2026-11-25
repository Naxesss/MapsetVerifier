using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckUnicodeTests
{
    private static List<Issue> Run(OsuBuilder builder)
    {
        using var context = CheckTestContext.CreateFromOsu(
            builder.WithDefaultTiming().WithDefaultHitObject()
        );

        return context.RunGeneralCheck<CheckUnicode>();
    }

    [Fact]
    public void RomanisedFields_AreFine_WithAnyUnicodeTitle()
    {
        Assert.Empty(Run(new OsuBuilder().Title("~Title~ (2)").TitleUnicode("タイトル")));
    }

    [Fact]
    public void UnicodeInRomanisedFields_IsAProblem_NamingTheCharacters()
    {
        var issue = Assert.Single(Run(new OsuBuilder().Title("Café été")));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("Romanized title", issue.message);
        Assert.Contains("those being \"ééé\"", issue.message);

        Assert.Contains(
            "Romanized artist",
            Assert.Single(Run(new OsuBuilder().Artist("é"))).message
        );
        Assert.Contains("Creator", Assert.Single(Run(new OsuBuilder().Creator("é"))).message);
    }

    [Fact]
    public void UnicodeInADifficultyName_IsOnlyAWarning()
    {
        Assert.Equal(
            Issue.Level.Warning,
            Assert.Single(Run(new OsuBuilder().Version("Étude"))).level
        );
    }
}
