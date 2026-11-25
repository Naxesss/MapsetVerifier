using MapsetVerifier.Checks.AllModes.Settings;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckDefaultColoursTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData("SkinPreference:My Skin", null, true)]
    [InlineData("SkinPreference:Default", null, false)]
    [InlineData(null, "Combo1 : 255,0,0", false)]
    public void WithoutColours_UnlessTheDefaultSkinIsPreferred(
        string? general,
        string? colour,
        bool expected
    )
    {
        var builder = new OsuBuilder().WithDefaultTiming().WithDefaultHitObject();
        if (colour != null)
            builder.Colours(colour);

        var code = builder.Build();
        if (general != null)
            code = OsuText.General(code, general);

        using var context = CheckTestContext.CreateFromOsu(code);

        Assert.Equal(expected, context.RunBeatmapCheck<CheckDefaultColours>("Test").Count == 1);
    }
}
