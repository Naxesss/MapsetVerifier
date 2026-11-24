using MapsetVerifier.Checks.Mania.Spread;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Spread;

public class CheckMaxChordSizeTests
{
    private static List<Issue> Run(string version, int keys, int chord)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version(version)
                .CircleSize(keys)
                .WithDefaultTiming()
                .HitObjects(
                    Enumerable
                        .Range(0, chord)
                        .Select(i => TestHitObjects.Circle(1000, x: (int)((i + 0.5) * 512 / keys)))
                )
                .HitObjects(TestHitObjects.Circle(2000))
        );

        return context.RunBeatmapCheck<CheckSeven>(version);
    }

    // "Avoid chords with more than 2 / 3 / 4 notes."
    [Theory]
    [InlineData("Easy", 4, 2)]
    [InlineData("Easy", 7, 2)]
    [InlineData("Normal", 4, 2)]
    [InlineData("Normal", 7, 3)]
    [InlineData("Hard", 4, 3)]
    [InlineData("Hard", 7, 4)]
    public void LargerChords_AreWarnedAbout(string version, int keys, int allowed)
    {
        Assert.Empty(Run(version, keys, allowed));

        if (allowed == keys)
            return;

        var issue = Assert.Single(Run(version, keys, allowed + 1));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains($"more than {allowed} notes in {keys}K", issue.message);
    }

    [Theory]
    [InlineData("Insane", 7, 6, 0)]
    [InlineData("Insane", 7, 7, 1)]
    [InlineData("Normal", 3, 3, 0)] // Not a keymode with a limit.
    [InlineData("Hard", 3, 3, 0)]
    public void OtherKeymodesAndDifficulties(string version, int keys, int chord, int expected)
    {
        Assert.Equal(expected, Run(version, keys, chord).Count);
    }

    [Theory]
    [InlineData(1000, 1)]
    [InlineData(1001, 0)]
    public void ChordsAreNotesAtOneTime(int time, int expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .Version("Easy")
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, 64),
                    TestHitObjects.Circle(1000, 192),
                    TestHitObjects.Circle(time, 320),
                    TestHitObjects.Circle(2000, 64)
                )
        );

        Assert.Equal(expected, context.RunBeatmapCheck<CheckSeven>("Easy").Count);
    }
}
