using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckContentUsagePermissionsTests
{
    private static List<Issue> Run(string artist, string source = "", string tags = "")
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "a.osu",
                new OsuBuilder()
                    .Artist(artist)
                    .Title("Song")
                    .Version("Hard")
                    .Source(source)
                    .Tags(tags)
                    .WithDefaultTiming()
                    .WithDefaultHitObject()
                    .Build()
            ),
        ]);

        return context.RunGeneralCheck<CheckContentUsagePermissions>();
    }

    [Fact]
    public void DisallowedArtist_IsAProblem()
    {
        var issue = Assert.Single(Run("kamome sano"));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("does not allow", issue.message);
    }

    [Fact]
    public void ArtistWithExceptions_ShowsTheConditions()
    {
        var issue = Assert.Single(Run("Lusumi"));

        Assert.Contains("execution_program", issue.message);
    }

    [Fact]
    public void DisallowedSource_IsFound()
    {
        Assert.Single(Run("Some Composer", "DJMAX RESPECT"));
    }

    [Fact]
    public void RestrictedNameOnlyInTags_IsMinor()
    {
        var issue = Assert.Single(Run("Kanata.N", tags: "sdvx megarex electronic"));

        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("MEGAREX", issue.message);
    }

    [Fact]
    public void RestrictedArtistInBothArtistAndTags_IsReportedOnce()
    {
        var issue = Assert.Single(Run("kamome sano", tags: "kamome sano electronic"));

        Assert.Equal(Issue.Level.Problem, issue.level);
    }

    [Fact]
    public void UnrestrictedArtist_HasNoIssues()
    {
        Assert.Empty(Run("Camellia"));
    }
}
