using MapsetVerifier.Checks.Standard.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.Spread;

/// <summary>
///     All maps here are 180 BPM, so 1/1 is 333 ms, and CS 4, so a circle is 73 px wide. Sliders of 140 px
///     last one beat.
/// </summary>
public class OsuOverlapChecksTests
{
    private const int Beat = 333;

    [Fact]
    public void SliderGoingBackAlongTheSamePath_IsReported()
    {
        using var context = Create("Hard", Line(1000, 100, 240), Line(1000 + Beat * 2, 240, 100));

        var issue = Assert.Single(BodyIssues(context, Beatmap.Difficulty.Hard));
        Assert.Contains("ending 1 beats earlier (min 2 beats)", issue.message);
        Assert.Equal(Issue.Level.Warning, issue.level);
    }

    [Fact]
    public void ShorterSliderUnderALongerOne_IsReported()
    {
        using var context = Create(
            "Hard",
            Line(1000, 100, 380, length: 280),
            Line(1000 + Beat * 3, 310, 170)
        );

        Assert.Single(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void LongerSliderUnderAShorterOne_IsNotReported()
    {
        using var context = Create(
            "Hard",
            Line(1000, 310, 170),
            Line(1000 + Beat * 2, 380, 100, length: 280)
        );

        Assert.Empty(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void CurvedSliderGoingBack_IsReported()
    {
        using var context = Create(
            "Hard",
            // Half a circle with a radius of 50 px, first one way and then the other.
            TestHitObjects.Slider(1000, "P", "150:142|200:192", length: 157, x: 100),
            TestHitObjects.Slider(1000 + Beat * 2, "P", "150:142|100:192", length: 157, x: 200)
        );

        Assert.Single(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Theory]
    [InlineData("Easy", Beatmap.Difficulty.Easy, 4, true)]
    [InlineData("Easy", Beatmap.Difficulty.Easy, 5, false)]
    [InlineData("Normal", Beatmap.Difficulty.Normal, 3, true)]
    [InlineData("Normal", Beatmap.Difficulty.Normal, 4, false)]
    [InlineData("Hard", Beatmap.Difficulty.Hard, 2, true)]
    [InlineData("Hard", Beatmap.Difficulty.Hard, 3, false)]
    [InlineData("Insane", Beatmap.Difficulty.Insane, 0.5, true)]
    [InlineData("Insane", Beatmap.Difficulty.Insane, 1, false)]
    public void GapLimit_FollowsTheLevel(
        string version,
        Beatmap.Difficulty difficulty,
        double gapBeats,
        bool isReported
    )
    {
        // AR 3 keeps objects on screen for longer than 4 beats.
        using var context = Create(
            new Settings(ApproachRate: 3),
            version,
            Line(1000, 100, 240),
            Line((int)(1000 + Beat + gapBeats * Beat), 240, 100)
        );

        Assert.Equal(isReported, BodyIssues(context, difficulty).Count > 0);
    }

    [Fact]
    public void SliderAppearingAfterTheFirstIsGone_IsNotReported()
    {
        // At AR 9 objects are only on screen for 600 ms.
        using var context = Create(
            new Settings(ApproachRate: 9),
            "Hard",
            Line(1000, 100, 240),
            Line(1000 + Beat * 3, 240, 100)
        );

        Assert.Empty(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void SliderStackedOnACopy_IsNotReported()
    {
        using var context = Create("Hard", Line(1000, 100, 240), Line(1000 + Beat * 2, 100, 240));

        Assert.Empty(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void PartlyCoveredSlider_IsNotReported()
    {
        using var context = Create(
            "Hard",
            Line(1000, 100, 240),
            Line(1000 + Beat * 2, 240, 80, 160)
        );

        Assert.Empty(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void CrossingSliders_AreNotReported()
    {
        using var context = Create(
            "Hard",
            Line(1000, 100, 240),
            TestHitObjects.Slider(1000 + Beat * 2, "L", "170:262", length: 140, x: 170, y: 122)
        );

        Assert.Empty(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void ReversedSliderUnderAnother_IsReported()
    {
        using var context = Create(
            "Hard",
            Line(1000, 100, 240),
            TestHitObjects.Slider(1000 + Beat * 2, "L", "100:192", slides: 2, length: 140, x: 240)
        );

        Assert.Single(BodyIssues(context, Beatmap.Difficulty.Hard));
    }

    [Theory]
    [InlineData("Expert")]
    [InlineData("Some name")]
    public void BodyOverlap_OtherLevelsAndGuessedLevels_AreSkipped(string version)
    {
        using var context = Create(version, Line(1000, 100, 240), Line(1000 + Beat * 2, 240, 100));
        var difficulty = context.GetBeatmap(version).GetDifficulty();

        Assert.Empty(BodyIssues(context, difficulty));
    }

    [Theory]
    [InlineData(Beat, 50, true)]
    [InlineData(Beat, 70, false)]
    [InlineData(Beat, 80, false)]
    [InlineData(Beat / 2, 50, false)]
    [InlineData(Beat * 4, 50, false)]
    public void CirclesFollowingEachOther(int gap, int distance, bool isReported)
    {
        using var context = Create(
            "Easy",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1000 + gap, 100 + distance)
        );

        Assert.Equal(isReported, EndIssues(context).Count > 0);
    }

    [Fact]
    public void HalfBeatOfASlowSong_IsNotReported()
    {
        // 1/2 at 110 BPM is 273 ms, which counts as 1/1 at 180 BPM.
        using var context = Create(
            new Settings(Bpm: 110),
            "Normal",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1273, 150)
        );

        Assert.Empty(EndIssues(context));
    }

    [Fact]
    public void SliderTailOverlappingNextCircle_IsReported()
    {
        using var context = Create(
            "Normal",
            Line(1000, 100, 240),
            TestHitObjects.Circle(1000 + Beat * 2, 270)
        );

        Assert.Equal(Issue.Level.Minor, Assert.Single(EndIssues(context)).level);
    }

    [Fact]
    public void CircleOverlappingOneFurtherBack_IsOnlyReportedIfMostlyCovered()
    {
        using var covered = Create(
            "Normal",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1000 + Beat, 250),
            TestHitObjects.Circle(1000 + Beat * 2, 115)
        );
        using var touching = Create(
            "Normal",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1000 + Beat, 250),
            TestHitObjects.Circle(1000 + Beat * 2, 140)
        );

        Assert.Single(EndIssues(covered));
        Assert.Empty(EndIssues(touching));
    }

    [Fact]
    public void VisibleStack_IsNotReported()
    {
        using var context = Create(
            new Settings(StackLeniency: 0.7f),
            "Normal",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1000 + Beat, 100)
        );

        Assert.Empty(EndIssues(context));
    }

    [Fact]
    public void ManyOverlaps_AreReportedTogether()
    {
        using var context = Create(
            "Easy",
            Enumerable
                .Range(0, 7)
                .Select(i => TestHitObjects.Circle(1000 + i * Beat, 60 + i * 50))
                .ToArray()
        );

        Assert.Contains("in 6 places", Assert.Single(EndIssues(context)).message);
    }

    [Fact]
    public void EndOverlap_GuessedLevels_AreSkipped()
    {
        using var context = Create(
            "Some name",
            TestHitObjects.Circle(1000, 100),
            TestHitObjects.Circle(1000 + Beat, 150)
        );

        Assert.Empty(EndIssues(context));
    }

    private static string Line(int time, int fromX, int toX, double length = 140) =>
        TestHitObjects.Slider(time, "L", $"{toX}:192", length: length, x: fromX);

    private static List<Issue> BodyIssues(
        CheckTestContext context,
        Beatmap.Difficulty difficulty
    ) =>
        context
            .RunBeatmapCheck<CheckSliderBodyOverlap>(Version(context))
            .Where(issue => issue.AppliesToDifficulty(difficulty))
            .ToList();

    private static List<Issue> EndIssues(CheckTestContext context) =>
        context.RunBeatmapCheck<CheckObjectOverlap>(Version(context));

    private static string Version(CheckTestContext context) =>
        context.BeatmapSet.Beatmaps[0].MetadataSettings.version;

    private static CheckTestContext Create(string version, params string[] hitObjects) =>
        Create(new Settings(), version, hitObjects);

    private static CheckTestContext Create(
        Settings settings,
        string version,
        params string[] hitObjects
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Version(version)
                .AR(settings.ApproachRate)
                .StackLeniency(settings.StackLeniency)
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000 / settings.Bpm))
                .HitObjects(hitObjects)
        );

    private record Settings(double Bpm = 180, float ApproachRate = 5, float StackLeniency = 0.1f);
}
