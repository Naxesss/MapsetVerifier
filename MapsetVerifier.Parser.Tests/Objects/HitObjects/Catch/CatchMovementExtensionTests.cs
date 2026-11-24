using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects.Catch;

public class CatchMovementExtensionTests
{
    private static ICatchHitObject Fruit(
        int time,
        int x,
        CatchMovementType movement = CatchMovementType.Walk,
        CatchNoteDirection direction = CatchNoteDirection.None,
        float toHyper = 0,
        float toDash = 0
    )
    {
        var beatmap = CatchTestBeatmap.Create(4, CatchTestBeatmap.Circle(time, x));
        var fruit = (ICatchHitObject)beatmap.HitObjects[0];
        fruit.MovementType = movement;
        fruit.NoteDirection = direction;
        fruit.DistanceToHyper = toHyper;
        fruit.DistanceToDash = toDash;

        return fruit;
    }

    [Theory]
    // Salad: dashes from 125 to 249 ms.
    [InlineData(Beatmap.Difficulty.Normal, CatchMovementType.Dash, 124, false)]
    [InlineData(Beatmap.Difficulty.Normal, CatchMovementType.Dash, 125, true)]
    [InlineData(Beatmap.Difficulty.Normal, CatchMovementType.Dash, 249, true)]
    [InlineData(Beatmap.Difficulty.Normal, CatchMovementType.Dash, 250, false)]
    [InlineData(Beatmap.Difficulty.Normal, CatchMovementType.Hyperdash, 125, false)]
    // Platter: dashes from 62 to 124 ms and hyperdashes from 125 to 249 ms.
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Dash, 61, false)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Dash, 62, true)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Dash, 124, true)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Dash, 125, false)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Hyperdash, 124, false)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Hyperdash, 125, true)]
    [InlineData(Beatmap.Difficulty.Hard, CatchMovementType.Hyperdash, 250, false)]
    // Rain: both from 62 to 124 ms.
    [InlineData(Beatmap.Difficulty.Insane, CatchMovementType.Dash, 62, true)]
    [InlineData(Beatmap.Difficulty.Insane, CatchMovementType.Hyperdash, 124, true)]
    [InlineData(Beatmap.Difficulty.Insane, CatchMovementType.Hyperdash, 125, false)]
    // Walks are never higher-snapped, nor is anything in a Cup or an Overdose.
    [InlineData(Beatmap.Difficulty.Insane, CatchMovementType.Walk, 62, false)]
    [InlineData(Beatmap.Difficulty.Easy, CatchMovementType.Dash, 125, false)]
    [InlineData(Beatmap.Difficulty.Expert, CatchMovementType.Dash, 62, false)]
    public void HigherSnapped_FollowsTheRankingCriteria(
        Beatmap.Difficulty difficulty,
        CatchMovementType movement,
        int ms,
        bool expected
    )
    {
        var current = Fruit(1000, 100, movement);
        var next = Fruit(1000 + ms, 300);

        Assert.Equal(expected, current.IsHigherSnapped(next, difficulty));
    }

    [Theory]
    [InlineData(2000, 3000, true)]
    [InlineData(2000, 3004, true)]
    [InlineData(2000, 2996, true)]
    [InlineData(2000, 3005, false)]
    [InlineData(2000, 2995, false)]
    public void SameSnap_AllowsFourMsOfDifference(int second, int third, bool expected)
    {
        Assert.Equal(
            expected,
            CatchExtensions.IsSameSnap(Fruit(1000, 0), Fruit(second, 0), Fruit(third, 0))
        );
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-50, 0)]
    [InlineData(180, 50)] // "Around half the pixels".
    [InlineData(300, 100)]
    [InlineData(1000, 100)]
    public void CurvedDistance_GrowsFromNothingToTheMaximum(double ms, float expected)
    {
        Assert.Equal(expected, CatchExtensions.GetCurvedDistance(ms, 100), 2f);
    }

    [Fact]
    public void TriggerDistance_IsHowFarPastTheThresholdTheNextObjectIs()
    {
        var right = Fruit(
            1000,
            100,
            CatchMovementType.Hyperdash,
            CatchNoteDirection.Right,
            toHyper: 160
        );
        var left = Fruit(
            1000,
            300,
            CatchMovementType.Hyperdash,
            CatchNoteDirection.Left,
            toHyper: -160
        );

        Assert.Equal(40, right.GetTriggerDistance(Fruit(1200, 300)));
        Assert.Equal(40, left.GetTriggerDistance(Fruit(1200, 100)));
        Assert.Equal(0.25f, right.GetCurrentTriggerDistance(Fruit(1200, 300)));
    }

    [Fact]
    public void TriggerDistance_WithoutThatMovementOrTarget_IsZero()
    {
        var dash = Fruit(
            1000,
            100,
            CatchMovementType.Dash,
            CatchNoteDirection.Right,
            toHyper: 160,
            toDash: 50
        );

        Assert.Equal(0, dash.GetTriggerDistance(Fruit(1200, 300)));
        Assert.Equal(0, dash.GetDashTriggerDistance(null));
        Assert.Equal(150, dash.GetDashTriggerDistance(Fruit(1200, 300)));
        Assert.Equal(3, dash.GetCurrentDashTriggerDistance(Fruit(1200, 300)));
    }

    [Fact]
    public void CatchHitObjects_OptionallyIncludeSliderParts()
    {
        var beatmap = CatchTestBeatmap.Create(
            4,
            CatchTestBeatmap.Circle(500, 100),
            // Two beats long with a reverse: a drop per span, one repeat and the tail.
            "0,100,1000,2,0,L|380:100,2,280,0|0|0,0:0|0:0|0:0,0:0:0:0:",
            CatchTestBeatmap.Spinner(4000, 5000)
        );

        Assert.Equal(3, beatmap.GetCatchHitObjects(false).Count);

        var parts = beatmap.GetCatchHitObjects(true).OfType<JuiceStream.JuiceStreamPart>().ToList();

        Assert.Equal(
            ["Drop", "Slider repeat", "Drop", "Slider tail"],
            parts.Select(part => part.GetNoteTypeName())
        );
        Assert.Equal([1500, 2000, 2500, 3000], parts.Select(part => Math.Round(part.Time)));
        Assert.Equal([140, 280, 140, 0], parts.Select(part => (int)Math.Round(part.Position.X)));
        Assert.Empty(CatchExtensions.GetCatchHitObjects(null, true));
    }

    [Fact]
    public void Timestamps_PointAtTheSliderOfAPart_Once()
    {
        var beatmap = CatchTestBeatmap.Create(
            4,
            CatchTestBeatmap.Circle(500, 100),
            "0,100,1000,2,0,L|380:100,1,280,0|0,0:0|0:0,0:0:0:0:"
        );
        var objects = beatmap.GetCatchHitObjects(true);

        Assert.Equal("00:00:500 (1,2) - ", CatchExtensions.GetTimestamps([.. objects, null]));
        Assert.Equal("00:01:000 (2) - ", CatchExtensions.GetTimestamps(objects[^1]));
    }

    [Fact]
    public void ScaledBpm_IsThatOfTheRedLineAtTheObject()
    {
        // 120 bpm.
        var beatmap = CatchTestBeatmap.Create(4, CatchTestBeatmap.Circle(1000, 100));

        Assert.Equal(0.5, beatmap.GetScaledBpm((ICatchHitObject)beatmap.HitObjects[0]), 6);
    }
}
