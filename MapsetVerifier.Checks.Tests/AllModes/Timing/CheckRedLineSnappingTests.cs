using MapsetVerifier.Checks.AllModes.Timing;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckRedLineSnappingTests
{
    [Fact]
    public void FlagsObjectSnappedToCurrentLineButNotUpcomingMisalignedRedLine()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Red Line Snap")
                .TimingPoints("0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0")
                .HitObjects("256,192,125,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("upcoming red line at 00:00:175", issue.message);
    }

    [Fact]
    public void DoesNotFlagWhenUpcomingRedLineAlignsWithCurrentGrid()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Red Line Snap")
                .TimingPoints("0,500,4,2,0,100,1,0", "2000,500,4,2,0,100,1,0")
                .HitObjects("256,192,1875,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhenUpcomingRedLineIsBeyondLookahead()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Red Line Snap")
                .TimingPoints("0,500,4,2,0,100,1,0", "250,500,4,2,0,100,1,0")
                .HitObjects("256,192,125,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhenAlreadyUnsnappedOnCurrentTiming()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Red Line Snap")
                .TimingPoints("0,500,4,2,0,100,1,0", "175,500,4,2,0,100,1,0")
                .HitObjects("256,192,127,1,0,0:0:0:0:")
        );

        var issues = context.RunBeatmapCheck<CheckRedLineSnapping>("Test");

        Assert.Empty(issues);
    }

    [Fact]
    public void DoesNotFlagWhatIsSnappedToBothLines()
    {
        // A beat off the downbeats, so the grid is the same.
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .TimingPoints("0,500,4,2,0,100,1,0", "2500,500,4,2,0,100,1,0")
                .HitObjects("256,192,2437,1,0,0:0:0:0:")
        );

        Assert.Empty(context.RunBeatmapCheck<CheckRedLineSnapping>("Test"));
    }

    [Theory]
    [InlineData("2000,500,4,2,0,100,1,0")] // A downbeat.
    [InlineData("2000,250,4,2,0,100,1,0")] // As above, at another speed.
    [InlineData("2000,500,3,2,0,100,1,0")]
    public void DoesNotFlagWhatIsSnappedToEither(string line)
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("Red Line Snap")
                .TimingPoints("0,500,4,2,0,100,1,0", line)
                .HitObjects("256,192,1938,1,0,0:0:0:0:")
        );

        Assert.Empty(context.RunBeatmapCheck<CheckRedLineSnapping>("Test"));
    }
}
