using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
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
}
