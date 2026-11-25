using MapsetVerifier.Checks.AllModes.HitSounds;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.HitSounds;

public class CheckHitSoundsTests
{
    private static List<Issue> Run(
        IEnumerable<string> hitObjects,
        string[]? timingPoints = null,
        string[]? events = null
    )
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints(timingPoints ?? [TestTimingPoints.Uninherited(0, 500)])
                .Events(events ?? [])
                .HitObjects(hitObjects)
        );

        return context.RunBeatmapCheck<CheckHitSounds>("Test");
    }

    /// <summary> A circle each second, with a clap on those at the given seconds. </summary>
    private static IEnumerable<string> Circles(int seconds, params int[] claps) =>
        Enumerable
            .Range(0, seconds + 1)
            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: claps.Contains(i) ? 8 : 0));

    [Fact]
    public void NoHitSounds_IsAProblem()
    {
        var issue = Assert.Single(Run(Circles(20)));

        Assert.Equal(Issue.Level.Problem, issue.level);
        Assert.Contains("no hit sounds", issue.message);
    }

    [Theory]
    [InlineData(10, null)]
    [InlineData(13, Issue.Level.Warning)]
    [InlineData(36, Issue.Level.Warning)]
    [InlineData(37, Issue.Level.Problem)]
    public void LongTimeWithoutFeedback(int seconds, Issue.Level? expected)
    {
        var issues = Run(Circles(seconds, 0, seconds));

        Assert.Equal(expected, issues.SingleOrDefault()?.level);
        Assert.All(issues, issue => Assert.Contains($"({seconds} s)", issue.message));
    }

    [Fact]
    public void FewObjects_DoNotNeedFeedback()
    {
        Assert.Empty(
            Run([
                TestHitObjects.Circle(0, hitSound: 8),
                TestHitObjects.Circle(20000),
                TestHitObjects.Circle(40000, hitSound: 8),
            ])
        );
    }

    [Fact]
    public void SamplesetChange_IsFeedback()
    {
        Assert.Empty(
            Run(
                Circles(20, 0, 20),
                [
                    TestTimingPoints.Uninherited(0, 500, sampleSet: 2),
                    TestTimingPoints.Inherited(10000, -100, sampleSet: 3),
                ]
            )
        );
    }

    [Fact]
    public void Breaks_DoNotNeedFeedback()
    {
        var circles = Enumerable
            .Range(0, 8)
            .Concat(Enumerable.Range(23, 8))
            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i is 0 or 30 ? 8 : 0))
            .ToList();

        Assert.Empty(Run(circles, events: ["2,7200,21800"]));
        Assert.Single(Run(circles));
    }

    [Fact]
    public void SliderEdges_AreFeedback()
    {
        var slider = TestHitObjects.Slider(10000, slides: 2, length: 140, edgeSounds: "0|8|0");

        Assert.Empty(
            Run(
                Circles(9, 0)
                    .Append(slider)
                    .Concat(
                        Enumerable
                            .Range(12, 9)
                            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i == 20 ? 8 : 0))
                    )
            )
        );
    }

    [Theory]
    [InlineData(7500, 22500, null)]
    [InlineData(7500, 22500, "2,7200,21800")]
    [InlineData(7600, 21000, "2,7200,22500")]
    public void Spinners_DoNotNeedFeedback(int start, int end, string? @break)
    {
        var circles = Enumerable
            .Range(0, 8)
            .Concat(Enumerable.Range(23, 8))
            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i is 0 or 30 ? 8 : 0))
            .ToList();
        circles.Insert(8, TestHitObjects.Spinner(start, end));

        Assert.Empty(Run(circles, events: @break == null ? null : [@break]));
    }

    [Theory]
    [InlineData("slider")]
    [InlineData("spinner")]
    public void Breaks_StartAfterTheObjectBefore(string before)
    {
        var objects = Enumerable
            .Range(0, 7)
            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i == 0 ? 8 : 0))
            .Append(
                before == "slider"
                    ? TestHitObjects.Slider(7000, length: 140)
                    : TestHitObjects.Spinner(7000, 7500)
            )
            .Concat(
                Enumerable
                    .Range(23, 8)
                    .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i == 30 ? 8 : 0))
            );

        Assert.Empty(Run(objects, events: ["2,7700,21800"]));
    }

    [Fact]
    public void BreakBeforeTheFirstObjectsOrAfterTheLast_ChangesNothing()
    {
        Assert.Single(Run(Circles(20, 0, 20), events: ["2,-5000,-1000", "2,21000,25000"]));
    }

    [Fact]
    public void LongSectionBeforeABreak_IsStillFound()
    {
        var circles = Enumerable
            .Range(0, 21)
            .Concat(Enumerable.Range(40, 3))
            .Select(i => TestHitObjects.Circle(i * 1000, hitSound: i is 0 or 42 ? 8 : 0));

        var issue = Assert.Single(Run(circles, events: ["2,20200,38800"]));

        Assert.Contains("(20 s)", issue.message);
    }

    private static List<Issue> RunMania(string[] events, string file = "")
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Parser.Objects.Beatmap.Mode.Mania)
                .WithDefaultTiming()
                .Events(events)
                .HitObjects(
                    Enumerable
                        .Range(0, 21)
                        .Select(i =>
                            TestHitObjects.Circle(
                                i * 1000,
                                x: 64,
                                hitSound: i is 0 or 20 ? 8 : 0,
                                hitSample: i == 10 ? "0:0:0:0:" + file : "0:0:0:0:"
                            )
                        )
                )
        );

        return context.RunBeatmapCheck<CheckHitSounds>("Test");
    }

    [Fact]
    public void Mania_StoryboardedAndOwnFiles_AreFeedback()
    {
        Assert.Single(RunMania([]));
        Assert.Empty(RunMania([], "clap.wav"));
        Assert.Empty(RunMania(["Sample,10500,0,\"clap.wav\",60"]));
        Assert.Single(RunMania(["Sample,14500,0,\"clap.wav\",60"]));
    }
}
