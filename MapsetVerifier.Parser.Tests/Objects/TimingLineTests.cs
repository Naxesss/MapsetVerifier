using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class TimingLineTests
{
    private static TimingLine CreateLine(string code) => new(code.Split(','), null!);

    [Theory]
    // The 0.1x - 10x range is only what the editor lets you type; hand-edited files go beyond it
    // and the game honours them, so parsing must not clamp.
    [InlineData("77115,-1072.64774244139,4,1,0,100,0,1", 0.09322725070244958)]
    [InlineData("0,-12.5,4,2,0,100,0,0", 8)]
    [InlineData("0,-8,4,2,0,100,0,0", 12.5)]
    [InlineData("0,-100,4,2,0,100,0,0", 1)]
    public void GetSvMult_DoesNotClampInheritedLines(string code, double expected) =>
        Assert.Equal(expected, CreateLine(code).SvMult, 12);

    [Fact]
    public void GetSvMult_IsOneForUninheritedLines() =>
        Assert.Equal(1, CreateLine("1010,375,4,1,0,100,1,0").SvMult);

    [Theory]
    // A non-negative beat length on an inherited line carries no sv multiplier, and naively
    // inverting it would divide by zero.
    [InlineData("0,0,4,2,0,100,0,0")]
    [InlineData("0,375,4,2,0,100,0,0")]
    public void GetSvMult_IsOneForInheritedLinesWithoutNegativeBeatLength(string code) =>
        Assert.Equal(1, CreateLine(code).SvMult);
}
