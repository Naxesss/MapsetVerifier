using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckSpacedNameTagsTests
{
    [Theory]
    [InlineData("jazz -[M o c h a]- english", "-[M o c h a]-", "-[M_o_c_h_a]-")]
    [InlineData("a b c jazz", "a b c", "a_b_c")]
    [InlineData("jazz -[M o c h a]-", "-[M o c h a]-", "-[M_o_c_h_a]-")]
    public void FlagsSpreadOutName(string tags, string name, string expected)
    {
        var warning = Assert.Single(Run(tags));

        Assert.Equal(Issue.Level.Warning, warning.level);
        Assert.Contains($"\"{name}\" in the tags should be \"{expected}\"", warning.message);
    }

    [Theory]
    [InlineData("jazz -[M_o_c_h_a]- english")]
    [InlineData("c c# english")]
    [InlineData("2 4 8 16 v 5")]
    [InlineData("夜 に 駆 け る")]
    [InlineData("rock & a / b")]
    [InlineData("")]
    public void DoesNotFlagOtherTags(string tags)
    {
        Assert.Empty(Run(tags));
    }

    private static List<Issue> Run(string tags)
    {
        using var context = CheckTestContext.CreateFromOsu(new OsuBuilder().Tags(tags));
        return context.RunGeneralCheck<CheckSpacedNameTags>();
    }

    [Theory]
    [InlineData("aé b c")]
    [InlineData("a b")]
    public void OtherLettersOrTooFew_AreNoSpreadOutName(string tags)
    {
        Assert.Empty(Run(tags));
    }

    [Theory]
    [InlineData("a2 b c d")]
    [InlineData("aあ b c")]
    public void DigitsAndOtherScripts_AreNoSpreadOutName(string tags)
    {
        Assert.DoesNotContain(
            Run(tags),
            issue => issue.message.Contains("a2") || issue.message.Contains("aあ")
        );
    }
}
