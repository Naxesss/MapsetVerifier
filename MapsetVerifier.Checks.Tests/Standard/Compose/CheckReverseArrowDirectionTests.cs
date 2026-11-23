using MapsetVerifier.Checks.Standard.Compose;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Compose;

public class CheckReverseArrowDirectionTests
{
    // To the right, with a hook of 10 px back to the left and down at the end.
    private const string HookedNodes = "200:100|200:100|192:106";
    private const double HookedLength = 110;

    [Fact]
    public void StraightSlider_IsNotReported()
    {
        using var context = Create(Slider("L", "200:100", 100));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void CurvedSlider_IsNotReported()
    {
        using var context = Create(Slider("P", "150:150|200:100", 157, slides: 3));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Theory]
    [InlineData(Beatmap.Difficulty.Easy, Issue.Level.Problem)]
    [InlineData(Beatmap.Difficulty.Normal, Issue.Level.Problem)]
    [InlineData(Beatmap.Difficulty.Hard, Issue.Level.Problem)]
    [InlineData(Beatmap.Difficulty.Insane, Issue.Level.Warning)]
    [InlineData(Beatmap.Difficulty.Expert, Issue.Level.Warning)]
    [InlineData(Beatmap.Difficulty.Ultra, Issue.Level.Warning)]
    public void HiddenHook_IsReported(Beatmap.Difficulty difficulty, Issue.Level level)
    {
        using var context = Create(Slider("B", HookedNodes, HookedLength));

        var issue = Assert.Single(Run(context, difficulty));
        Assert.Equal(level, issue.level);
    }

    [Fact]
    public void UnknownLevel_IsReportedAsWarning()
    {
        using var context = Create(Slider("B", HookedNodes, HookedLength), version: "Test");

        var issue = Assert.Single(context.RunBeatmapCheck<CheckReverseArrowDirection>("Test"));
        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void HiddenHookWithoutReverse_IsNotReported()
    {
        using var context = Create(Slider("B", HookedNodes, HookedLength, slides: 1));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void VisibleHook_IsNotReported()
    {
        // Comes out from beneath the arrow before it turns.
        using var context = Create(Slider("B", "200:100|200:100|168:124", 140));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void HookAtStart_IsReportedFromTheSecondReverse(int slides, bool isReported)
    {
        using var context = Create(Slider("B", "92:106|92:106|200:106", 118, slides));

        Assert.Equal(isReported, Run(context, Beatmap.Difficulty.Easy).Count == 1);
    }

    [Fact]
    public void SliderShorterThanItsArrow_IsNotReported()
    {
        using var context = Create(Slider("B", "110:100|110:100|104:100", 16));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Theory]
    // Small arcs, of which the arrow follows the curl.
    [InlineData("129:101|98:113", 53.3)]
    [InlineData("87:101|69:116", 49.5)]
    public void CurledSliderBeneathItsArrow_IsNotReported(string nodes, double length)
    {
        using var context = Create(Slider("P", nodes, length));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Fact]
    public void ShortHookBeneathItsArrow_IsNotReported()
    {
        using var context = Create(Hook(140, length: 25));

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Theory]
    // The circles at both ends overlap, but the arrow still points to the side the slider is on.
    [InlineData(95, 45, Issue.Level.Minor)]
    // The arrow points to the other side.
    [InlineData(140, 45, Issue.Level.Problem)]
    // Long enough to see the path in between.
    [InlineData(95, 200, Issue.Level.Problem)]
    public void SliderBarelyComingOut_IsMinorUnlessPointingAway(
        int hookAngle,
        double length,
        Issue.Level level
    )
    {
        using var context = Create(Hook(hookAngle, length));

        Assert.Equal(level, Assert.Single(Run(context, Beatmap.Difficulty.Easy)).level);
    }

    [Theory]
    [InlineData(55, null)]
    [InlineData(75, Issue.Level.Minor)]
    [InlineData(100, Issue.Level.Problem)]
    public void AngleCloseToLimit_IsMinor(int hookAngle, Issue.Level? level)
    {
        using var context = Create(Hook(hookAngle, length: 200));

        Assert.Equal(level, Run(context, Beatmap.Difficulty.Easy).SingleOrDefault()?.level);
    }

    [Fact]
    public void UnknownLevelCloseToLimit_IsMinor()
    {
        using var context = Create(Hook(75, length: 200), version: "Test");

        var issue = Assert.Single(context.RunBeatmapCheck<CheckReverseArrowDirection>("Test"));
        Assert.Equal(Issue.Level.Minor, issue.level);
    }

    [Fact]
    public void ArrowFollowingThePathFurtherOut_IsNotReported()
    {
        // Bends over beneath the arrow, which does point at where the slider goes after that.
        using var context = Create(
            Slider("B", "132:110|149:78|149:78|151:41|151:41|131:70|141:54", 125)
        );

        Assert.Empty(Run(context, Beatmap.Difficulty.Easy));
    }

    [Theory]
    // A perfect curve of two or four nodes is drawn as something else.
    [InlineData("P", "200:100", 100)]
    [InlineData("P", "150:150|200:100|250:150", 150)]
    [InlineData("P", "150:100|200:100", 100)]
    [InlineData("C", "150:150|200:100|250:150", 150)]
    [InlineData("B3", "150:150|200:100|250:150", 120)]
    [InlineData("B", "150:150|150:150|L|200:100", 120)]
    public void OtherCurves_AreRead(string type, string nodes, double length)
    {
        using var context = Create(Slider(type, nodes, length, slides: 3));
        var slider = context.GetBeatmap("Test's Easy").HitObjects.OfType<Slider>().Single();

        var path = OsuReverseArrows.GetPath(slider);
        var pathLength = path.Zip(path.Skip(1), (a, b) => (b - a).Length).Sum();

        Assert.Equal(length, pathLength, 0.5);
    }

    /// <summary> A straight slider ending in a hook of 12 px, at the given angle from going straight on. </summary>
    private static string Hook(int angle, double length)
    {
        var radians = angle * Math.PI / 180;
        var corner = 100 + (int)length;
        var x = corner + (int)Math.Round(12 * Math.Cos(radians));
        var y = 100 + (int)Math.Round(12 * Math.Sin(radians));
        var hookLength = Math.Sqrt(Math.Pow(x - corner, 2) + Math.Pow(y - 100, 2));

        return Slider("B", $"{corner}:100|{corner}:100|{x}:{y}", length + hookLength);
    }

    private static string Slider(string type, string nodes, double length, int slides = 2) =>
        TestHitObjects.Slider(1000, type, nodes, slides, length, x: 100, y: 100);

    private static List<Issue> Run(CheckTestContext context, Beatmap.Difficulty difficulty) =>
        context
            .RunBeatmapCheck<CheckReverseArrowDirection>("Test's Easy")
            .Where(issue => issue.AppliesToDifficulty(difficulty))
            .ToList();

    private static CheckTestContext Create(string slider, string version = "Test's Easy") =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Version(version).WithDefaultTiming().HitObjects(slider)
        );
}
