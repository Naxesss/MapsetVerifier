using MapsetVerifier.Checks.Taiko.Timing;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Timing;

public class CheckKiaiConsistencyTests
{
    private static List<Issue> Run(params int[] kiaiStarts)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            kiaiStarts.Select(
                (start, i) =>
                    (
                        $"{i}.osu",
                        new OsuBuilder()
                            .Mode(Beatmap.Mode.Taiko)
                            .Version($"Diff {i}")
                            .TimingPoints(
                                TestTimingPoints.Uninherited(0, 500),
                                TestTimingPoints.Inherited(start, -100, effects: 1),
                                TestTimingPoints.Inherited(8000, -100)
                            )
                            .WithDefaultHitObject()
                            .Build()
                    )
            )
        );

        return context.RunBeatmapSetCheck<CheckKiaiConsistency>();
    }

    [Fact]
    public void SameKiai_IsFine()
    {
        Assert.Empty(Run(4000, 4000));
    }

    [Fact]
    public void DifferentKiai_GroupsTheDifficulties()
    {
        Assert.Equal(
            ["Group 1: (Diff 0, Diff 2)", "Group 2: (Diff 1)"],
            Run(4000, 6000, 4000).Select(issue => issue.message)
        );
    }
}
