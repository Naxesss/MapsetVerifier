using MapsetVerifier.Checks.Catch.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose;

public class CheckDashPatternsTests
{
    private const CatchMovementType Walk = CatchMovementType.Walk;
    private const CatchMovementType Dash = CatchMovementType.Dash;
    private const CatchMovementType Hyper = CatchMovementType.Hyperdash;

    /// <summary> Fruits at the given gaps and x positions, the first of them starting the given movements. </summary>
    private static List<Issue> Run(int[] gaps, int[] xs, params CatchMovementType[] movements)
    {
        var time = 1000;
        var hitObjects = new List<string> { TestHitObjects.Circle(time, xs[0]) };
        for (var i = 0; i < gaps.Length; ++i)
            hitObjects.Add(TestHitObjects.Circle(time += gaps[i], xs[i + 1]));

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Mode(Beatmap.Mode.Catch).WithDefaultTiming().HitObjects(hitObjects)
        );
        CatchObjects.Set(context, movements);

        return context.RunBeatmapCheck<CheckDashPatterns>("Test");
    }

    private static string[] Messages(List<Issue> issues, Beatmap.Difficulty difficulty) =>
        issues
            .Where(issue => issue.AppliesToDifficulty(difficulty))
            .Select(issue => issue.message)
            .ToArray();

    private static int[] Line(int count) =>
        Enumerable.Range(0, count).Select(i => i * 60).ToArray();

    private static int[] Gaps(int count, int ms) => Enumerable.Repeat(ms, count).ToArray();

    private static CatchMovementType[] Many(int count, CatchMovementType type) =>
        Enumerable.Repeat(type, count).ToArray();

    [Theory]
    [InlineData(125, false, false)]
    [InlineData(124, true, false)]
    [InlineData(62, true, false)]
    [InlineData(61, true, true)]
    public void Gap_OfADash(int ms, bool salad, bool platter)
    {
        var issues = Run([ms, 1000], Line(3), Dash);

        Assert.Equal(
            salad,
            Messages(issues, Beatmap.Difficulty.Normal).Any(m => m.Contains("less than 125"))
        );
        Assert.Equal(
            platter,
            Messages(issues, Beatmap.Difficulty.Hard).Any(m => m.Contains("less than 62"))
        );
    }

    [Fact]
    public void Salad_AllowsTwoBasicSnappedDashesInARow()
    {
        Assert.Empty(Run([300, 300, 1000], Line(4), Dash, Dash));

        Assert.Contains(
            "3 basic-snapped dashes in a row, more than 2",
            Assert.Single(
                Messages(
                    Run([300, 300, 300, 1000], Line(5), Dash, Dash, Dash),
                    Beatmap.Difficulty.Normal
                )
            )
        );
    }

    [Fact]
    public void Platter_AllowsFourBasicSnappedDashesInARow()
    {
        Assert.Empty(
            Messages(Run([.. Gaps(4, 300), 1000], Line(6), Many(4, Dash)), Beatmap.Difficulty.Hard)
        );
        Assert.Contains(
            "5 basic-snapped dashes in a row, more than 4",
            Assert.Single(
                Messages(
                    Run([.. Gaps(5, 300), 1000], Line(7), Many(5, Dash)),
                    Beatmap.Difficulty.Hard
                )
            )
        );
    }

    [Fact]
    public void AWalk_EndsTheRow()
    {
        Assert.Empty(Run([300, 300, 300, 300, 300, 1000], Line(7), Dash, Dash, Walk, Dash, Dash));
    }

    [Fact]
    public void Salad_HigherSnappedDash_MustBeFollowedByAWalk()
    {
        Assert.Empty(Run([200, 300, 1000], Line(4), Dash, Walk));
        Assert.Contains(
            Messages(Run([200, 300, 1000], Line(4), Dash, Dash), Beatmap.Difficulty.Normal),
            message => message.Contains("not followed by a walk")
        );
    }

    [Fact]
    public void Salad_HigherSnappedDash_FollowedByAntiflow_IsMinor()
    {
        // A step back the catcher reaches without moving isn't antiflow.
        Assert.Empty(Run([200, 300, 1000], [0, 200, 190, 190], Dash, Walk));

        var issue = Assert.Single(Run([200, 300, 1000], [0, 300, 100, 100], Dash, Walk));
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Contains("antiflow", issue.message);
    }

    [Fact]
    public void Salad_BasicSnappedDashesOfDifferentSnaps_AreAWarning()
    {
        var issue = Assert.Single(Run([300, 500, 1000], Line(4), Dash, Dash));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("different snaps", issue.message);
    }

    [Fact]
    public void Platter_HigherSnappedDashes_AreLimitedToTwo_GoingTheSameWay()
    {
        Assert.Empty(Messages(Run([100, 100, 1000], Line(4), Dash, Dash), Beatmap.Difficulty.Hard));
        Assert.Contains(
            "3 dashes in a row with a higher-snapped one",
            Assert.Single(
                Messages(
                    Run([100, 100, 100, 1000], Line(5), Dash, Dash, Dash),
                    Beatmap.Difficulty.Hard
                )
            )
        );
        Assert.Contains(
            "change direction",
            Assert.Single(
                Messages(Run([100, 100, 1000], [0, 100, 0, 0], Dash, Dash), Beatmap.Difficulty.Hard)
            )
        );
    }

    [Fact]
    public void Platter_HyperdashesOfDifferentSnaps_AreAProblem()
    {
        Assert.Empty(Run([300, 300, 1000], Line(4), Hyper, Hyper));

        var issue = Assert.Single(Run([300, 500, 1000], Line(4), Hyper, Hyper));
        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("Hyperdashes of different snaps", issue.message);
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void Row_AtTheEndOfTheMap_IsFoundToo()
    {
        Assert.Contains(
            Messages(Run([300, 300, 300], Line(4), Dash, Dash, Dash), Beatmap.Difficulty.Normal),
            message => message.Contains("3 basic-snapped")
        );
    }

    [Theory]
    [InlineData(100, 200, 100, 0, true)]
    [InlineData(100, 200, 100, 150, false)]
    [InlineData(100, 200, 300, 0, false)]
    [InlineData(100, 100, 300, 0, false)]
    [InlineData(100, 300, 300, 0, false)]
    public void Antiflow_IsTurningAround(int a, int b, int c, double minBack, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, a),
                    TestHitObjects.Circle(1200, b),
                    TestHitObjects.Circle(1400, c)
                )
        );
        var objects = context.GetBeatmap("Test").HitObjects.Cast<ICatchHitObject>().ToList();

        Assert.Equal(
            expected,
            CheckDashPatterns.IsAntiflow(objects[0], objects[1], objects[2], minBack)
        );
        Assert.False(CheckDashPatterns.InSameSlider(objects[0], objects[1]));
    }

    [Fact]
    public void PartsOfASlider_AreInTheSameOne()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "400:192", 2, 140, 100),
                    TestHitObjects.Slider(3000, "L", "400:192", 1, 140, 100)
                )
        );
        var sliders = context.GetBeatmap("Test").HitObjects.OfType<JuiceStream>().ToList();

        Assert.True(CheckDashPatterns.InSameSlider(sliders[0], sliders[0].Parts[0]));
        Assert.True(CheckDashPatterns.InSameSlider(sliders[0].Parts[0], sliders[0].Parts[^1]));
        Assert.False(CheckDashPatterns.InSameSlider(sliders[0].Parts[^1], sliders[1].Parts[0]));
        Assert.False(CheckDashPatterns.InSameSlider(sliders[0].Parts[^1], sliders[1]));
    }

    /// <summary> Movements within and out of sliders count as well. </summary>
    [Fact]
    public void Gap_OfADashFromASliderTail()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "400:192"),
                    TestHitObjects.Circle(1450)
                )
        );
        CatchObjects.Set(context);
        var beatmap = context.GetBeatmap("Test");
        var slider = beatmap.HitObjects.OfType<JuiceStream>().Single();

        foreach (var part in slider.Parts)
            part.MovementType = Walk;

        slider.Parts[^1].MovementType = Dash;

        Assert.Contains(
            Messages(context.RunBeatmapCheck<CheckDashPatterns>("Test"), Beatmap.Difficulty.Normal),
            message => message.Contains("less than 125")
        );
    }
}
