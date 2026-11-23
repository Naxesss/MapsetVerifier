using MapsetVerifier.Checks.AllModes.General.Metadata;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckMetadataLengthTests
{
    [Fact]
    public void ShortMetadata_ProducesNoIssues()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Song")
                .Artist("Artist")
                .Tags("some tags")
                .WithDefaultTiming()
                .WithDefaultHitObject()
        );

        Assert.Empty(context.RunGeneralCheck<CheckMetadataLength>());
    }

    [Fact]
    public void TitleOf81Characters_IsAllowed()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title(new string('a', 81)).WithDefaultTiming().WithDefaultHitObject()
        );

        Assert.Empty(context.RunGeneralCheck<CheckMetadataLength>());
    }

    [Fact]
    public void TitleOver81Characters_IsReported()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title(new string('a', 82)).WithDefaultTiming().WithDefaultHitObject()
        );

        var issues = context.RunGeneralCheck<CheckMetadataLength>();

        Assert.Contains(
            issues,
            issue => issue.message.StartsWith("Romanised title is 82 characters long")
        );
    }

    [Fact]
    public void TagsOver1000Characters_IsReported()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Tags(new string('a', 1001)).WithDefaultTiming().WithDefaultHitObject()
        );

        var issue = Assert.Single(context.RunGeneralCheck<CheckMetadataLength>());
        Assert.Contains("1001 characters", issue.message);
    }
}
