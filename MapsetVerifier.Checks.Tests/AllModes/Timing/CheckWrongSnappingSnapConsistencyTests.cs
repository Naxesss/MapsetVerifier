using MapsetVerifier.Checks.AllModes.Timing;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckWrongSnappingSnapConsistencyTests
{
    [Theory]
    [InlineData(4, 8, true)]
    [InlineData(3, 6, true)]
    [InlineData(4, 6, false)]
    [InlineData(16, 4, false)]
    [InlineData(16, 12, false)]
    [InlineData(2, 3, false)]
    [InlineData(1, 4, false)]
    public void IsMinorSnapInconsistency_ClassifiesDivisorPairs(
        int divisorThis,
        int divisorOther,
        bool expectedMinor
    )
    {
        Assert.Equal(expectedMinor, CheckWrongSnapping.IsMinorSnapInconsistency(divisorThis, divisorOther));
    }
}
