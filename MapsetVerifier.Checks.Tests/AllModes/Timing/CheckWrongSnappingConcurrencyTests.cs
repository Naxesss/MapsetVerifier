using MapsetVerifier.Checks.AllModes.Timing;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Timing;

public class CheckWrongSnappingConcurrencyTests
{
    // Check instances are shared between runs, so two mapsets checked at the same time
    // must not see each other's results or fail with "Collection was modified".
    [Fact]
    public void SharedInstanceGivesSameResultsWhenRunConcurrently()
    {
        using var quarterSet = CreateSetWithRareDivisor("Quarter", offsetMs: 150);
        using var thirdSet = CreateSetWithRareDivisor("Third", offsetMs: 200);

        var check = new CheckWrongSnapping();
        var expectedQuarter = Messages(check, quarterSet);
        var expectedThird = Messages(check, thirdSet);

        Assert.Contains(expectedQuarter, message => message.Contains("1/4"));
        Assert.Contains(expectedThird, message => message.Contains("1/3"));

        Parallel.For(
            0,
            200,
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            iteration =>
            {
                if (iteration % 2 == 0)
                    Assert.Equal(expectedQuarter, Messages(check, quarterSet));
                else
                    Assert.Equal(expectedThird, Messages(check, thirdSet));
            }
        );
    }

    private static List<string> Messages(CheckWrongSnapping check, CheckTestContext context) =>
        check.GetIssues(context.BeatmapSet).Select(issue => issue.message).ToList();

    // Mostly 1/1 circles with a few circles offset onto a rarely used divisor.
    private static CheckTestContext CreateSetWithRareDivisor(string title, int offsetMs)
    {
        var hitObjects = Enumerable
            .Range(1, 400)
            .Select(beat =>
            {
                var time = beat * 600 + (beat % 50 == 0 ? offsetMs : 0);
                return $"256,192,{time},1,0,0:0:0:0:";
            })
            .ToArray();

        return CheckTestContext.CreateFromOsuFiles([
            (
                "normal.osu",
                new OsuBuilder()
                    .Title(title)
                    .Version("Normal")
                    .TimingPoints("0,600,4,2,0,100,1,0")
                    .HitObjects(hitObjects)
                    .Build()
            ),
        ]);
    }
}
