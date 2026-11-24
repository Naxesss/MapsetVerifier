using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckLuminosityTests
{
    private static List<Issue> Run(bool kiai, params string[] colours)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Colours(colours)
                .TimingPoints(
                    TestTimingPoints.Uninherited(0, 500),
                    TestTimingPoints.Inherited(500, -100, effects: kiai ? 1 : 0)
                )
                // The first new combo is the second colour.
                .HitObjects("256,192,1000,5,0,0:0:0:0:", "256,192,2000,5,0,0:0:0:0:")
        );

        return context.RunBeatmapCheck<CheckLuminosity>("Test");
    }

    // Greys have their own value as luminosity.
    [Theory]
    [InlineData("29,29,29", Issue.Level.Problem)]
    [InlineData("31,31,31", Issue.Level.Warning)]
    [InlineData("42,42,42", Issue.Level.Warning)]
    [InlineData("44,44,44", null)]
    [InlineData("255,255,255", null)] // Only too bright in kiai.
    public void DarkColour(string colour, Issue.Level? expected)
    {
        var issues = Run(false, "Combo1 : 100,100,100", $"Combo2 : {colour}");

        Assert.Equal(expected, issues.SingleOrDefault()?.level);

        if (expected != null)
            Assert.Contains("Combo colour 1", issues[0].message);
    }

    [Theory]
    [InlineData("29,29,29", Issue.Level.Problem)]
    [InlineData("42,42,42", Issue.Level.Warning)]
    [InlineData("44,44,44", null)]
    public void DarkSliderBorder(string colour, Issue.Level? expected)
    {
        Assert.Equal(expected, Run(false, $"SliderBorder : {colour}").SingleOrDefault()?.level);
    }

    [Fact]
    public void BrightColour_InKiai_IsAWarningWhereItIsFirstUsed()
    {
        var issue = Assert.Single(Run(true, "Combo1 : 100,100,100", "Combo2 : 255,255,255"));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("00:01:000", issue.message);
    }

    [Fact]
    public void BrightColour_OnlyWarnsIfUsedInKiai()
    {
        // With two new combos only the second and third are reached.
        Assert.Empty(
            Run(true, "Combo1 : 255,255,255", "Combo2 : 100,100,100", "Combo3 : 90,90,90")
        );
    }
}
