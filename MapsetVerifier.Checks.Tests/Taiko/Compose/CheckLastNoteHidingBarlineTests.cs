using MapsetVerifier.Checks.Taiko.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Compose;

public class CheckLastNoteHidingBarlineTests
{
    // Barlines every 2000 ms.
    private static List<Issue> Run(string lastObject)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Taiko)
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Circle(1000), lastObject)
        );

        return context.RunBeatmapCheck<CheckLastNoteHidingBarline>("Test");
    }

    [Theory]
    [InlineData(4000)]
    [InlineData(3990)]
    [InlineData(5000)]
    public void LastNote_OnOrAwayFromABarline_IsFine(int time)
    {
        Assert.Empty(Run(TestHitObjects.Circle(time)));
    }

    [Fact]
    public void LastNote_OneMsEarly_IsAProblem()
    {
        Assert.Equal(Issue.Level.Problem, Assert.Single(Run(TestHitObjects.Circle(3999))).level);
    }

    [Fact]
    public void LastSpinner_OneMsEarly_IsMinor()
    {
        Assert.Equal(
            Issue.Level.Minor,
            Assert.Single(Run(TestHitObjects.Spinner(2000, 3999))).level
        );
    }
}
