using System.Globalization;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class TimingLineParsingTests
{
    [Fact]
    public void AllFieldsOfAnUninheritedLine()
    {
        var line = Assert.IsType<UninheritedLine>(
            Assert.Single(TestBeatmap.WithTiming("440,476.190476190476,3,2,5,40,1,9").TimingLines)
        );

        Assert.Equal(440, line.Offset);
        Assert.Equal(476.190476190476, line.msPerBeat);
        Assert.Equal(126, line.bpm, 6);
        Assert.Equal(3, line.Meter);
        Assert.Equal(HitSample.SamplesetType.Soft, line.Sampleset);
        Assert.Equal(5, line.CustomIndex);
        Assert.Equal(40, line.Volume);
        Assert.True(line.Uninherited);
        Assert.Equal(1, line.SvMult);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(1, true, false)]
    [InlineData(8, false, true)]
    [InlineData(9, true, true)]
    public void Effects_AreBitFlags(int effects, bool kiai, bool omitsBarLine)
    {
        var line = TestBeatmap.WithTiming($"0,500,4,2,0,100,1,{effects}").TimingLines[0];

        Assert.Equal(kiai, line.Kiai);
        Assert.Equal(omitsBarLine, line.OmitsBarLine);
    }

    [Theory]
    [InlineData("-100", 1)]
    [InlineData("-50", 2)]
    [InlineData("-200", 0.5)]
    [InlineData("-133.333333333333", 0.75)]
    [InlineData("-1000", 0.1)]
    // Out of the editor's range, but still what the file says.
    [InlineData("-5", 20)]
    // Only negative values are a multiplier.
    [InlineData("0", 1)]
    [InlineData("500", 1)]
    public void InheritedLine_SpeedIsANegativeInversePercentage(string beatLength, double expected)
    {
        var line = TestBeatmap.WithTiming($"0,{beatLength},4,2,0,100,0,0").TimingLines[0];

        Assert.IsType<InheritedLine>(line);
        Assert.Equal(expected, line.SvMult, 6);
    }

    [Fact]
    public void OldFormat_WithoutInheritanceOrEffects_IsUninherited()
    {
        var line = TestBeatmap.WithTiming("0,500,4,1,0,100").TimingLines[0];

        Assert.IsType<UninheritedLine>(line);
        Assert.False(line.Kiai);
        Assert.False(line.OmitsBarLine);
    }

    [Fact]
    public void OldestFormat_WithOnlyOffsetAndBeatLength_IsParsed()
    {
        // As in v3 files; everything after the beat length came later.
        var line = Assert.IsType<UninheritedLine>(
            TestBeatmap.WithTiming("1230,352.941176470588").TimingLines[0]
        );

        Assert.Equal(1230, line.Offset);
        Assert.Equal(4, line.Meter);
    }

    [Fact]
    public void Decimals_DoNotDependOnTheCulture()
    {
        var before = CultureInfo.CurrentCulture;

        try
        {
            var decimalComma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            decimalComma.NumberFormat.NumberDecimalSeparator = ",";
            decimalComma.NumberFormat.NumberGroupSeparator = ".";
            CultureInfo.CurrentCulture = decimalComma;

            var line = TestBeatmap.WithTiming("10.5,333.25,4,2,0,55.5,1,0").TimingLines[0];

            Assert.Equal(10.5, line.Offset);
            Assert.Equal(333.25, ((UninheritedLine)line).msPerBeat);
            Assert.Equal(55.5f, line.Volume);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>From the ranking criteria on scaling BPM.</summary>
    [Theory]
    [InlineData("500", 0.5)] // 120 bpm
    [InlineData("333.333333333333", 1)] // 180 bpm
    [InlineData("250", 2)] // 240 bpm
    public void ScaledBpm_FollowsTheRankingCriteria(string msPerBeat, double expected)
    {
        var line = TestBeatmap.WithTiming($"0,{msPerBeat},4,2,0,100,1,0").TimingLines[0];

        Assert.Equal(expected, ((UninheritedLine)line).GetScaledBpm(), 6);
    }
}
