using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckMarkerSpacingTests
{
    private static List<Issue> Run(string artist)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Artist(artist).WithDefaultTiming().WithDefaultHitObject()
        );

        // Without a unicode artist the romanised one stands in for it, so everything shows up twice.
        return context
            .RunGeneralCheck<CheckMarkerSpacing>()
            .Where(issue => !issue.message.Contains("artist unicode"))
            .ToList();
    }

    [Theory]
    [InlineData("A feat. B")]
    [InlineData("A (CV: B)")]
    [InlineData("A vs. B")]
    [InlineData("A, B & C")]
    [InlineData("A (feat. B)")]
    public void RightSpacing_IsFine(string artist)
    {
        Assert.Empty(Run(artist));
    }

    [Theory]
    [InlineData("A feat.B", "whitespace after \"feat.\"")]
    [InlineData("Afeat. B", "whitespace before \"feat.\"")]
    [InlineData("A vs.B", "whitespace after \"vs.\"")]
    [InlineData("A (CV:B)", "whitespace after \"CV:\"")]
    [InlineData("A(CV: B)", "whitespace before \"(CV:\"")]
    [InlineData("A CV: B", "\"(\" before \"CV:\"")]
    [InlineData("A,B", "whitespace after \",\"")]
    public void MissingSpaceOrBracket_IsAProblem(string artist, string expected)
    {
        Assert.Contains(
            Run(artist),
            issue => issue.level == Issue.Level.Problem && issue.message.Contains(expected)
        );
    }

    [Theory]
    [InlineData("A&B")]
    [InlineData("A &B")]
    [InlineData("A& B")]
    public void Ampersand_IsOnlyAWarning(string artist)
    {
        var issues = Run(artist);

        Assert.NotEmpty(issues);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
    }

    [Theory]
    [InlineData("CVLTE")]
    [InlineData("DCVG")]
    public void CvWithinAName_IsNoMarker(string artist)
    {
        Assert.Empty(Run(artist));
    }

    [Theory]
    [InlineData("A(feat. B)")]
    [InlineData("Avs. B")]
    public void MissingSpaceBeforeAMarker_IsFound(string artist)
    {
        Assert.NotEmpty(Run(artist));
    }
}
