using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects.Catch;

/// <summary>
/// A movement is a dash when it cannot be played while only walking. That is measured two ways and
/// the stricter one decides: per movement, and across the pattern to catch spacing that drifts away
/// faster than walking can keep up with.
///
/// All numbers below assume CS 4, where the catcher catches fruit within 48.68px of its middle.
/// Every movement additionally gains 0.5px per millisecond of walking, minus the quarter frame of
/// grace (4.17ms) the game subtracts from the time between objects. Hyperdashes are left exactly as
/// the game calculates them and use its own wider 60.85px window instead.
///
/// Where these thresholds belong is settled by <see cref="BookmarkedDashTests"/>, which measures the
/// calculator against real difficulties where the mapper marked every dash.
/// </summary>
public class HitObjectDistanceCalculatorTests
{
    /// <summary>
    /// The same x-distance is a walk, a dash or a hyperdash purely depending on the snapping used.
    /// At 130px: 1/2 (250ms) allows 171px walking, 1/4 (125ms) allows 109px walking but 181px
    /// dashing, and 1/8 (62ms) only allows 118px even when dashing.
    /// </summary>
    [Theory]
    [InlineData(250, CatchMovementType.Walk)]
    [InlineData(125, CatchMovementType.Dash)]
    [InlineData(62, CatchMovementType.Hyperdash)]
    public void SameDistanceChangesMovementTypeWithSnapping(
        int timeBetween,
        CatchMovementType expected
    )
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 100),
            CatchTestBeatmap.Circle(1000 + timeBetween, 230)
        );

        Assert.Equal(expected, objects[0].MovementType);
    }

    [Theory]
    // 250ms allows 171.6px of walking and 306.68px of dashing.
    [InlineData(250, 150, CatchMovementType.Walk)]
    [InlineData(250, 200, CatchMovementType.Dash)]
    [InlineData(250, 320, CatchMovementType.Hyperdash)]
    // 125ms allows 109.1px of walking and 181.68px of dashing.
    [InlineData(125, 100, CatchMovementType.Walk)]
    [InlineData(125, 150, CatchMovementType.Dash)]
    [InlineData(125, 200, CatchMovementType.Hyperdash)]
    // 30ms allows 61.6px of walking and 86.68px of dashing.
    [InlineData(30, 55, CatchMovementType.Walk)]
    [InlineData(30, 70, CatchMovementType.Dash)]
    [InlineData(30, 95, CatchMovementType.Hyperdash)]
    public void ClassifiesMovementAroundWalkAndDashThresholds(
        int timeBetween,
        int distance,
        CatchMovementType expected
    )
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1000 + timeBetween, 50 + distance)
        );

        Assert.Equal(expected, objects[0].MovementType);
    }

    /// <summary>
    /// There is no margin around the boundary. Real maps were checked with one, and it only ever
    /// swallowed movements the mapper had marked as dashes without preventing a single wrong one.
    /// At 250ms the catcher covers exactly 171.6px while walking.
    /// </summary>
    [Theory]
    [InlineData(171, CatchMovementType.Walk)]
    [InlineData(172, CatchMovementType.Dash)]
    public void TheWalkingRangeBoundaryIsExact(int distance, CatchMovementType expected)
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 50 + distance)
        );

        Assert.Equal(expected, objects[0].MovementType);
    }

    /// <summary>
    /// A zigzag that creeps sideways drifts faster than walking speed even though every individual
    /// movement stays well inside the walking range, so only following the pattern as a whole
    /// reveals that the catcher cannot keep up.
    /// </summary>
    [Fact]
    public void SlowlyDriftingZigzagRequiresADash()
    {
        // Alternating +54 / -6 steps every 30ms, so 246px of net drift over 270ms. That is
        // 0.91px per millisecond, far beyond the 0.5px per millisecond the catcher can walk,
        // while no single step comes near the 61.6px a 30ms movement may cover.
        int[] positions = [50, 104, 98, 152, 146, 200, 194, 248, 242, 296];
        var objects = CalculateFor(
            positions.Select((x, index) => CatchTestBeatmap.Circle(1000 + index * 30, x)).ToArray()
        );

        Assert.Contains(objects, o => o.MovementType == CatchMovementType.Dash);
        Assert.DoesNotContain(objects, o => o.MovementType == CatchMovementType.Hyperdash);
    }

    /// <summary>
    /// Once the player dashes the pattern continues from wherever dashing could reach, rather than
    /// from the walking range that just ran out.
    /// </summary>
    [Fact]
    public void MovementAfterADashCanBeAWalkAgain()
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 300),
            CatchTestBeatmap.Circle(1500, 400)
        );

        Assert.Equal(CatchMovementType.Dash, objects[0].MovementType);
        Assert.Equal(CatchMovementType.Walk, objects[1].MovementType);
    }

    [Fact]
    public void LastObjectHasNoMovement()
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 400)
        );

        var last = objects[^1];
        Assert.Equal(CatchMovementType.Walk, last.MovementType);
        Assert.Equal(CatchNoteDirection.None, last.NoteDirection);
        Assert.Equal(float.PositiveInfinity, last.DistanceToHyper);
        Assert.Equal(float.PositiveInfinity, last.DistanceToDash);
    }

    [Fact]
    public void SpinnersNeverProduceADash()
    {
        var objects = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Spinner(1250, 1750),
            CatchTestBeatmap.Circle(2000, 500)
        );

        // Both the movement into and out of the spinner are ignored.
        Assert.Equal(CatchMovementType.Walk, objects[0].MovementType);
        Assert.Equal(float.PositiveInfinity, objects[0].DistanceToDash);
        Assert.Equal(CatchMovementType.Walk, objects[1].MovementType);
        Assert.Equal(float.PositiveInfinity, objects[1].DistanceToDash);
    }

    /// <summary>
    /// A dash is always beyond the walking range but never beyond the dashing range.
    /// </summary>
    [Fact]
    public void DistanceToDashIsNegativeOnlyWhenDashingIsRequired()
    {
        var walk = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 200)
        )[0];
        var dash = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 300)
        )[0];
        var hyper = CalculateFor(
            CatchTestBeatmap.Circle(1000, 50),
            CatchTestBeatmap.Circle(1250, 450)
        )[0];

        Assert.True(walk.DistanceToDash > 0);
        Assert.True(walk.DistanceToHyper > 0);

        Assert.True(dash.DistanceToDash < 0);
        Assert.True(dash.DistanceToHyper > 0);

        Assert.True(hyper.DistanceToDash < 0);
        Assert.True(hyper.DistanceToHyper < 0);
    }

    private static List<ICatchHitObject> CalculateFor(params string[] hitObjects) =>
        CatchTestBeatmap.Create(circleSize: 4, hitObjects).GetCatchHitObjects(true);
}
