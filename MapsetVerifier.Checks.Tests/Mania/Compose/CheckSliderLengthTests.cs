using MapsetVerifier.Checks.Mania.Compose;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Compose;

public class CheckSliderLengthTests
{
    [Theory]
    [InlineData(19, Issue.Level.Problem)]
    [InlineData(20, Issue.Level.Warning)]
    [InlineData(29, Issue.Level.Warning)]
    [InlineData(30, null)]
    public void ShortHoldNotes(int length, Issue.Level? expected)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Mania)
                .WithDefaultTiming()
                .HitObjects($"64,192,1000,128,0,{1000 + length}:0:0:0:0:")
        );

        Assert.Equal(
            expected,
            context.RunBeatmapCheck<CheckSliderLength>("Test").SingleOrDefault()?.level
        );
    }
}
