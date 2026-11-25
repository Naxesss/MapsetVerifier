using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckNinjaSpinnerTests
{
    private static List<Issue> Run(float od, int length)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .OD(od)
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Spinner(1000, 1000 + length))
        );

        return context.RunBeatmapCheck<CheckNinjaSpinner>("Test");
    }

    // Auto needs longer the higher the OD is.
    [Theory]
    [InlineData(5, 449, Issue.Level.Problem)]
    [InlineData(5, 450, Issue.Level.Warning)]
    [InlineData(5, 499, Issue.Level.Warning)]
    [InlineData(5, 500, null)]
    [InlineData(10, 534, Issue.Level.Problem)]
    [InlineData(10, 599, Issue.Level.Warning)]
    [InlineData(10, 600, null)]
    [InlineData(0, 364, Issue.Level.Problem)]
    [InlineData(0, 391, null)]
    public void LengthAutoNeeds_DependsOnOd(float od, int length, Issue.Level? expected)
    {
        Assert.Equal(expected, Run(od, length).SingleOrDefault()?.level);
    }
}
