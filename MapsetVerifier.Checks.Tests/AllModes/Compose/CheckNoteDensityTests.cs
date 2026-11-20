using MapsetVerifier.Checks.AllModes.Compose;
using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Compose;

public class CheckNoteDensityTests
{
    [Fact]
    public void Gaps_AreMeasuredIn180BpmBeats()
    {
        // 1/2 at 120 BPM is 250 ms, three quarters of a 180 BPM beat.
        using var context = CreateContext(Beatmap.Mode.Standard, bpm: 120, times: Every(250, 30));

        var gaps = RhythmUtils.GetGaps(context.GetBeatmap("Test"));

        Assert.Equal(29, gaps.Count);
        Assert.All(gaps, gap => Assert.Equal(0.75, gap.ReferenceBeats, 3));
    }

    [Theory]
    // 1/2 at 120 BPM is right on the 240 BPM breaking point, so it still counts as 1/1.
    [InlineData(120, 250, false)]
    // 1/2 at 180 BPM is clearly faster than 1/1.
    [InlineData(180, 167, true)]
    public void HalfBeat_CountsAsFasterThan1_1_DependingOnTempo(double bpm, int gapMs, bool faster)
    {
        using var context = CreateContext(Beatmap.Mode.Standard, bpm, Every(gapMs, 30));

        var gaps = RhythmUtils.GetGaps(context.GetBeatmap("Test"));

        Assert.All(gaps, gap => Assert.Equal(faster, gap.IsFasterThan(1)));
    }

    [Fact]
    public void LongPausesAndSpinners_AreNotGaps()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000d / 180))
                .HitObjects(
                    TestHitObjects.Circle(1000),
                    TestHitObjects.Circle(1333),
                    // A pause of more than 4 beats.
                    TestHitObjects.Circle(5000),
                    TestHitObjects.Spinner(5333, 6000),
                    TestHitObjects.Circle(6333),
                    TestHitObjects.Circle(6666)
                )
        );

        var gaps = RhythmUtils.GetGaps(context.GetBeatmap("Test"));

        Assert.Equal([1333d, 6666d], gaps.Select(gap => gap.Time));
    }

    [Fact]
    public void NormalOfMostly1_1_ProducesNoIssues()
    {
        using var context = CreateContext(Beatmap.Mode.Standard, 180, Every(333, 40));

        Assert.Empty(RunFor(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void NormalOfAll1_2_IsReported()
    {
        using var context = CreateContext(Beatmap.Mode.Standard, 180, Every(167, 40));

        var issue = Assert.Single(RunFor(context, Beatmap.Difficulty.Normal));
        Assert.Equal(Issue.Level.Minor, issue.level);
        Assert.Equal(
            "100% of the rhythm is faster than 1/1, while a Normal should mostly use 1/1, occasional 1/2, or slower rhythms.",
            issue.message
        );
    }

    [Fact]
    public void HardOfAll1_2_ProducesNoIssues()
    {
        using var context = CreateContext(Beatmap.Mode.Standard, 180, Every(167, 40));

        Assert.Empty(RunFor(context, Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void KantanOfAll1_1_IsReported()
    {
        using var context = CreateContext(Beatmap.Mode.Taiko, 180, Every(333, 40));

        var issue = Assert.Single(RunFor(context, Beatmap.Difficulty.Easy));
        Assert.Contains("faster than 2/1, while a Kantan", issue.message);
    }

    [Fact]
    public void CupOfAll1_2_IsReported()
    {
        using var context = CreateContext(Beatmap.Mode.Catch, 180, Every(167, 40));

        var issue = Assert.Single(RunFor(context, Beatmap.Difficulty.Easy));
        Assert.Contains("while a Cup should mostly use a 1/1 pattern", issue.message);
    }

    [Fact]
    public void ShortMaps_AreNotJudged()
    {
        using var context = CreateContext(Beatmap.Mode.Standard, 180, Every(167, 10));

        Assert.Empty(RunFor(context, Beatmap.Difficulty.Normal));
    }

    [Fact]
    public void GuessedDifficultyLevel_IsSkipped()
    {
        // "hs" doesn't name a level, so it would be guessed from star rating.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .Version("hs")
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000d / 180))
                .HitObjects(Every(167, 40).Select(time => TestHitObjects.Circle(time)))
        );

        Assert.Empty(new CheckNoteDensity().GetIssues(context.GetBeatmap("hs")));
    }

    private static List<Issue> RunFor(CheckTestContext context, Beatmap.Difficulty difficulty)
    {
        var beatmap = context.GetBeatmap("Test");
        beatmap.InterpretedDifficultyOverride = difficulty;

        return new CheckNoteDensity()
            .GetIssues(beatmap)
            .Where(issue =>
                issue.InterpretationPairs.Any(pair =>
                    pair.Key == "difficulty" && pair.Value == (int)difficulty
                )
            )
            .ToList();
    }

    private static IEnumerable<int> Every(int ms, int count) =>
        Enumerable.Range(0, count).Select(i => 1000 + i * ms);

    private static CheckTestContext CreateContext(
        Beatmap.Mode mode,
        double bpm,
        IEnumerable<int> times
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(mode)
                .TimingPoints(TestTimingPoints.Uninherited(0, 60000 / bpm))
                .HitObjects(times.Select(time => TestHitObjects.Circle(time)))
        );
}
