using MapsetVerifier.Checks.AllModes.General.Files;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Files;

public class CheckUpdateValidityTests
{
    private static List<Issue> Run(string fileName, params string[] extraFiles)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    fileName,
                    new OsuBuilder()
                        .Artist("AC/DC")
                        .Title("What?")
                        .Creator("Creator")
                        .Version("Hard")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFiles
        );

        return context.RunGeneralCheck<CheckUpdateValidity>();
    }

    [Theory]
    [InlineData("ACDC - What (Creator) [Hard].osu")]
    [InlineData("acdc - what (creator) [hard].osu")]
    public void NamedAfterItsMetadata_IsFine(string fileName)
    {
        Assert.Empty(Run(fileName));
    }

    [Fact]
    public void NamedOtherwise_IsAWarning_WithTheRightName()
    {
        var issue = Assert.Single(Run("ACDC - What (Creator) [Insane].osu"));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("should be named \"ACDC - What (Creator) [Hard].osu\"", issue.message);
    }

    [Fact]
    public void LongFileName_IsAWarning()
    {
        const string osu = "ACDC - What (Creator) [Hard].osu";

        Assert.Empty(Run(osu, new string('a', 126) + ".png"));

        var issue = Assert.Single(Run(osu, new string('a', 127) + ".png"));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("(131)", issue.message);
    }
}
