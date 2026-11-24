using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckMarkerFormatTests
{
    private static List<Issue> Run(string artist, string title = "Title")
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Artist(artist).Title(title).WithDefaultTiming().WithDefaultHitObject()
        );

        return context.RunGeneralCheck<CheckMarkerFormat>();
    }

    [Theory]
    [InlineData("A feat. B")]
    [InlineData("A (CV: B)")]
    [InlineData("A vs. B")]
    [InlineData("A")]
    // Only as words of their own.
    [InlineData("Aft Bft")]
    [InlineData("Feather")]
    [InlineData("The Cvs")]
    public void RightOrNoMarker_IsFine(string artist)
    {
        Assert.Empty(Run(artist));
    }

    [Theory]
    [InlineData("A ft. B", "feat.")]
    [InlineData("A feat B", "feat.")]
    [InlineData("A Feat. B", "feat.")]
    [InlineData("A (ft. B)", "feat.")]
    [InlineData("A (cv: B)", "CV:")]
    [InlineData("A (CV B)", "CV:")]
    [InlineData("A vs B", "vs.")]
    [InlineData("A VS. B", "vs.")]
    public void WrongMarker_IsAProblem(string artist, string marker)
    {
        var issues = Run(artist);

        // Without a unicode artist the romanised one stands in for it.
        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Problem, issue.level));
        Assert.Contains($"\"{marker}\" marker in Romanized artist", issues[0].message);
        Assert.Contains($"\"{marker}\" marker in Unicode artist", issues[1].message);
    }

    [Fact]
    public void Title_IsCheckedToo()
    {
        Assert.Contains("Romanized title", Run("A", "Song (ft. B)")[0].message);
    }
}
