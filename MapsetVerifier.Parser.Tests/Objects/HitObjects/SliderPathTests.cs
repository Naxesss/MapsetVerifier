using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using osu.Framework.Utils;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects;

public class SliderPathTests
{
    private static Slider CreateSlider(string hitObject)
    {
        var code = $"""
[General]
Mode: 0

[Metadata]
Version:Test

[Difficulty]
CircleSize:4
HPDrainRate:5
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:2.4
SliderTickRate:1

[TimingPoints]
0,333.333333333333,4,2,0,100,1,0

[HitObjects]
{hitObject}
""";

        return (Slider)new Beatmap(code, "song", "map.osu").HitObjects.First();
    }

    [Fact]
    public void RedAnchor_IsReachedBySampledPath()
    {
        // Two linear segments joined by a red anchor at 144:-23, which is the sharpest and most
        // extreme point of the path. Sampling at fixed intervals cuts such corners, so the anchor
        // has to be part of the curve the path is sampled from.
        var slider = CreateSlider(
            "150,39,57338,6,0,B|144:-23|144:-23|173:105,1,180,0|0,0:0|0:0,0:0:0:0:"
        );

        var closest = slider.PathPxPositions.Min(position =>
            Vector2.Distance(position, new Vector2(144, -23))
        );

        Assert.True(
            closest < 1,
            $"Closest sampled position to the red anchor was {closest} px off."
        );
    }

    [Fact]
    public void RedAnchorBeyondPixelLength_IsNotReachedBySampledPath()
    {
        // The slider is cut short well before the anchor at 144:-23, so the path never reaches it.
        var slider = CreateSlider(
            "150,39,57338,6,0,B|144:-23|144:-23|173:105,1,20,0|0,0:0|0:0,0:0:0:0:"
        );

        var closest = slider.PathPxPositions.Min(position =>
            Vector2.Distance(position, new Vector2(144, -23))
        );

        Assert.True(
            closest > 40,
            $"Closest sampled position to the red anchor was {closest} px off."
        );
    }

    [Fact]
    public void PathAroundRedAnchors_HasNoCoincidentPoints()
    {
        // Anything reading the angles between path points needs the points to be spread out, so
        // including anchors may not leave two points nearly on top of each other.
        var slider = CreateSlider(
            "256,100,57338,6,0,B|200:100|200:100|150:150|150:150|250:200|250:200|300:100,1,300,0|0,0:0|0:0,0:0:0:0:"
        );

        var positions = slider.PathPxPositions;

        // The last position is the end of the curve, which is appended regardless of how close it is.
        for (var i = 1; i < positions.Count - 1; ++i)
        {
            var gap = Vector2.Distance(positions[i - 1], positions[i]);

            Assert.True(gap > 0.2, $"Positions {i - 1} and {i} are only {gap} px apart.");
        }
    }

    [Fact]
    public void BSpline_IsParsedWithItsDegree()
    {
        // Lazer-exclusive curve type, written as "B" followed by its polynomial degree. Its nodes are
        // written with decimals, unlike the whole numbers stable is limited to.
        var slider = CreateSlider(
            "170.45354,78.15354,14777.777777777777,2,0,B4|169.4865:182.20056,2,100,0|0|0,1:0|1:0|1:0,1:0:0:0:"
        );

        Assert.Equal(Slider.Curve.BSpline, slider.CurveType);
        Assert.Equal(4, slider.CurveDegree);

        Assert.Equal(
            new[] { new Vector2(170.45354f, 78.15354f), new Vector2(169.4865f, 182.20056f) },
            slider.NodePositions
        );
    }

    [Fact]
    public void BSplineWithoutDegree_IsParsedAsBezier()
    {
        var slider = CreateSlider("150,39,57338,6,0,B|144:-23|173:105,1,180,0|0,0:0|0:0,0:0:0:0:");

        Assert.Equal(Slider.Curve.Bezier, slider.CurveType);
        Assert.Equal(0, slider.CurveDegree);
    }

    [Fact]
    public void BSplinePath_FollowsTheSplineRatherThanItsNodes()
    {
        // Unlike a bezier, a b-spline only passes through its first and last node, so the path has to
        // be converted into beziers before being sampled rather than being followed as one.
        var slider = CreateSlider(
            "100,100,57338,6,0,B3|200:50|300:150|400:50|500:150,1,250,0|0,0:0|0:0,0:0:0:0:"
        );

        var spline = PathApproximator.BSplineToPiecewiseLinear(
            slider.NodePositions.Select(node => new osuTK.Vector2(node.X, node.Y)).ToArray(),
            slider.CurveDegree
        );

        foreach (var position in slider.PathPxPositions)
        {
            var distance = DistanceToPath(position, spline);

            Assert.True(
                distance < 2,
                $"Sampled position {position} was {distance} px off the spline."
            );
        }

        // The second node is the sharpest part of the control polygon, which the spline rounds off by
        // a wide margin. Following the nodes as a bezier would not stray nearly as far from it.
        var closestToNode = slider.PathPxPositions.Min(position =>
            Vector2.Distance(position, slider.NodePositions[1])
        );

        Assert.True(closestToNode > 10, $"Path came within {closestToNode} px of the second node.");
    }

    [Fact]
    public void BSplineWithRedAnchor_IsSplitIntoSeparateSplines()
    {
        // A duplicated node both ends one segment and starts the next, so the path is made up of two
        // splines meeting at the anchor, which means the anchor itself is passed through.
        var slider = CreateSlider(
            "100,100,57338,6,0,B3|200:50|250:150|250:150|350:50|400:150,1,300,0|0,0:0|0:0,0:0:0:0:"
        );

        var closest = slider.PathPxPositions.Min(position =>
            Vector2.Distance(position, new Vector2(250, 150))
        );

        Assert.True(
            closest < 1,
            $"Closest sampled position to the red anchor was {closest} px off."
        );
    }

    [Fact]
    public void SampledPath_StaysWithinPixelLength()
    {
        var slider = CreateSlider(
            "150,39,57338,6,0,B|144:-23|144:-23|173:105,1,180,0|0,0:0|0:0,0:0:0:0:"
        );

        double length = 0;
        for (var i = 1; i < slider.PathPxPositions.Count; ++i)
            length += Vector2.Distance(slider.PathPxPositions[i - 1], slider.PathPxPositions[i]);

        // The path is sampled, so it is a little shorter than the curve it approximates, never longer.
        Assert.InRange(length, 170, 181);
    }

    /// <summary> Returns how far the given position is from the closest point on the given path. </summary>
    private static double DistanceToPath(Vector2 position, List<osuTK.Vector2> path)
    {
        var distance = double.MaxValue;

        for (var i = 1; i < path.Count; ++i)
        {
            var start = new Vector2(path[i - 1].X, path[i - 1].Y);
            var end = new Vector2(path[i].X, path[i].Y);

            var segment = end - start;
            var lengthSquared = segment.LengthSquared();

            // Project the position onto the segment, clamping to stay between its two ends.
            var fraction =
                lengthSquared == 0
                    ? 0
                    : Math.Clamp(Vector2.Dot(position - start, segment) / lengthSquared, 0, 1);

            distance = Math.Min(distance, Vector2.Distance(position, start + segment * fraction));
        }

        return distance;
    }
}
