using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckWrongSnappingRareTests
{
    /// <summary> Circles on 1/1 of 600 ms, and some of them followed by one a third of a beat later. </summary>
    private static List<Issue> Run(int beats, int thirds, int quarters = 0)
    {
        var hitObjects = Enumerable
            .Range(0, beats)
            .SelectMany(i =>
            {
                var objects = new List<string> { TestHitObjects.Circle(i * 600) };
                if (i < quarters)
                    objects.Add(TestHitObjects.Circle(i * 600 + 150));
                if (i >= quarters && i < quarters + thirds)
                    objects.Add(TestHitObjects.Circle(i * 600 + 200));

                return objects;
            });

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints(TestTimingPoints.Uninherited(0, 600))
                .HitObjects(hitObjects)
        );

        return context.RunBeatmapSetCheck<CheckWrongSnapping>();
    }

    [Fact]
    public void OnlyTheRareSnapIsNamed_WithEachOfItsTimes()
    {
        var issues = Run(100, 2, 60);

        var issue = Assert.Single(issues);
        Assert.Contains("1/3", issue.message);
        Assert.Contains("00:36:200", issue.message);
        Assert.Contains("00:36:800", issue.message);
        Assert.DoesNotContain("00:00:150", issue.message);
    }

    [Fact]
    public void CommonSnaps_AreFine()
    {
        Assert.Empty(Run(100, 40, 40));
    }

    [Theory]
    [InlineData(new double[] { 1000 }, 1000, true)]
    [InlineData(new double[] { 1002.5 }, 1000, true)]
    [InlineData(new double[] { 997.5 }, 1000, true)]
    [InlineData(new double[] { 1003 }, 1000, false)]
    [InlineData(new double[] { 997 }, 1000, false)]
    [InlineData(new double[] { 500, 990, 1001, 2000 }, 1000, true)]
    [InlineData(new double[0], 1000, false)]
    public void MatchingEdge_IsLessThanThreeMsAway(double[] others, double time, bool expected)
    {
        Assert.Equal(expected, CheckWrongSnapping.HasMatchingEdgeWithin(others, time));
    }

    [Theory]
    [InlineData(900, 4, "Warning")] // Under 0.5%, but more than a few.
    [InlineData(400, 10, "Minor")] // Under 5%.
    [InlineData(100, 5, "Minor")] // Only few.
    [InlineData(100, 3, "Warning")]
    [InlineData(100, 20, null)]
    public void RareSnaps_ByCountAndShare(int beats, int thirds, string? expected)
    {
        Assert.Equal(expected, Run(beats, thirds).SingleOrDefault()?.level.ToString());
    }

    /// <summary> Beats of 600 ms, with something at the given offset of the fifth in each difficulty. </summary>
    private static List<Issue> Compare(int easier, int harder)
    {
        string Build(string version, int offset, int distance) =>
            new OsuBuilder()
                .Version(version)
                .TimingPoints(TestTimingPoints.Uninherited(0, 600))
                .HitObjects(
                    Enumerable
                        .Range(0, 40)
                        .SelectMany(i =>
                            new[] { i * 600, i * 600 + 300 }.Concat(
                                i == 5 ? [i * 600 + offset] : Array.Empty<int>()
                            )
                        )
                        .Order()
                        .Select((time, i) => TestHitObjects.Circle(time, 50 + i % 2 * distance))
                )
                .Build();

        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build("Easier", easier, 20)),
            ("b.osu", Build("Harder", harder, 400)),
        ]);

        return context
            .RunBeatmapSetCheck<CheckWrongSnapping>()
            .Where(issue => issue.message.Contains("Harder"))
            .ToList();
    }

    [Theory]
    [InlineData(200, 150, Issue.Level.Minor, "1/3", "1/4")]
    [InlineData(150, 200, Issue.Level.Minor, "1/4", "1/3")]
    [InlineData(150, 75, Issue.Level.Minor, "1/4", "1/8")]
    [InlineData(200, 100, Issue.Level.Minor, "1/3", "1/6")]
    public void SnapOfAnotherKind_ThanTheHarderDifficulty(
        int easier,
        int harder,
        Issue.Level level,
        string snap,
        string otherSnap
    )
    {
        var issue = Assert.Single(Compare(easier, harder));

        Assert.Equal(level, issue.level);
        Assert.Contains(snap, issue.message);
        Assert.Contains(otherSnap, issue.message);
    }

    [Theory]
    [InlineData(150, 150)] // The same.
    [InlineData(75, 150)] // Simpler in the harder one.
    [InlineData(150, 152)] // Only an unsnap.
    [InlineData(50, 75)] // Simpler in the harder one, though just as close as a confused snap.
    [InlineData(38, 562)] // Nowhere near each other.
    public void SameOrUnrelatedSnaps_AreFine(int easier, int harder)
    {
        Assert.Empty(Compare(easier, harder));
    }
}
