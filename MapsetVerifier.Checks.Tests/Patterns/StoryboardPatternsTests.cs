using MapsetVerifier.Checks.Utils;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Patterns;

public class StoryboardPatternsTests
{
    private static Beatmap Map(IEnumerable<string> events, params string[] hitObjects) =>
        new(
            new OsuBuilder().WithDefaultTiming().Events(events).HitObjects(hitObjects).Build(),
            "song",
            $"{Guid.NewGuid():N}.osu"
        );

    private static Sprite Sprite(
        string[] commands,
        string origin = "Centre",
        int x = 320,
        int y = 240
    ) =>
        Map([$"Sprite,Pass,{origin},\"a.png\",{x},{y}", .. commands.Select(c => " " + c)]).Sprites[
            0
        ];

    [Theory]
    // A 100x100 image whose origin is just outside of the screen.
    [InlineData("TopLeft", -150, 240, false)]
    [InlineData("TopLeft", -250, 240, true)]
    [InlineData("CentreLeft", 760, 240, true)]
    [InlineData("CentreRight", 760, 240, false)]
    [InlineData("TopRight", 760, 240, false)]
    [InlineData("BottomLeft", 320, 500, false)]
    [InlineData("TopCentre", 320, 490, true)]
    [InlineData("BottomCentre", 320, 490, false)]
    [InlineData("BottomRight", 320, -10, true)]
    [InlineData("TopCentre", 320, -10, false)]
    [InlineData("Centre", 320, 525, false)]
    [InlineData("Centre", 320, 535, true)]
    [InlineData("Custom", 320, 560, false)]
    public void NeverOnScreen_DependsOnTheOrigin(string origin, int x, int y, bool expected)
    {
        var sprite = Sprite(["F,0,0,1000,1"], origin, x, y);

        Assert.Equal(expected, StoryboardPatterns.IsNeverOnScreen(sprite, 100, 100));
    }

    [Theory]
    [InlineData("M,0,0,1000,-300,240,-200,240", true)]
    [InlineData("M,0,0,1000,-300,240,320,240", false)]
    [InlineData("MX,0,0,1000,-300,-200", true)]
    [InlineData("MY,0,0,1000,600,700", true)]
    // Elastic and back easings go past where they end.
    [InlineData("MX,29,0,1000,-600,-200", false)]
    public void NeverOnScreen_DependsOnMovement(string command, bool expected)
    {
        Assert.Equal(
            expected,
            StoryboardPatterns.IsNeverOnScreen(Sprite(["F,0,0,1000,1", command]), 100, 100)
        );
    }

    [Theory]
    [InlineData("F,0,0,1000,1", true)]
    [InlineData("S,0,0,1000,1,3", true)]
    [InlineData("V,0,0,1000,1,1,3,1", true)]
    [InlineData("R,0,0,1000,0,1", false)]
    [InlineData("P,0,0,1000,H", false)]
    [InlineData("P,0,0,1000,V", false)]
    [InlineData("P,0,0,1000,A", true)]
    [InlineData("S,0,0,1000,-1", false)]
    [InlineData("V,0,0,1000,1,1,1,-1", false)]
    [InlineData("C,0,0,1000,255,255,255", true)]
    public void NeverOnScreen_DependsOnSizeAndTurning(string command, bool expected)
    {
        // Left of the screen by 10 px, with the image reaching further left.
        var sprite = Sprite(["F,0,0,1000,1", command], "CentreRight", -117);

        Assert.Equal(expected, StoryboardPatterns.IsNeverOnScreen(sprite, 100, 100));
    }

    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "T,HitSound,0,100" }, false)]
    [InlineData(new[] { "T,HitSound,0,100", " F,0,0,100,0,1" }, true)]
    [InlineData(new[] { "T,HitSound,0,100", " F,0,0,100,1,0" }, false)]
    [InlineData(new[] { "T,HitSound,0,100", " S,0,0,100,1,0" }, false)]
    [InlineData(new[] { "T,HitSound,0,100", " V,0,0,100,1,1,1,0" }, false)]
    [InlineData(new[] { "T,HitSound,0,100", " V,0,0,100,1,1,1,1" }, true)]
    // Hidden by the sprite's own commands once the trigger can't happen anymore.
    [InlineData(new[] { "F,0,0,500,1,0", "T,HitSound,0,100", " F,0,0,100,0,1" }, false)]
    [InlineData(new[] { "F,0,0,500,1,0", "T,HitSound,0,900", " F,0,0,100,0,1" }, true)]
    [InlineData(new[] { "F,0,0,500,1,0", "T,HitSound", " F,0,0,100,0,1" }, false)]
    [InlineData(new[] { "F,0,0,500,1,0", "T,HitSound,0,900", " M,0,0,100,0,0,9,9" }, false)]
    [InlineData(new[] { "F,0,0,500,0,1", "T,HitSound,0,100", " M,0,0,100,0,0,9,9" }, true)]
    public void StaysVisibleAfterTrigger(string[] commands, bool expected)
    {
        Assert.Equal(expected, StoryboardPatterns.StaysVisibleAfterTrigger(Sprite(commands)));
    }

    [Theory]
    [InlineData("HitSoundClap", 0d, 2000d, false)]
    [InlineData("HitSoundClap", 2000d, 0d, true)]
    [InlineData("HitSoundClap", 2000d, 3000d, true)]
    [InlineData("HitSoundClap", 0d, 500d, true)]
    [InlineData("HitSoundClap", null, null, false)]
    [InlineData("HitSoundKazoo", 0d, 2000d, true)]
    [InlineData("Passing", 2000d, 3000d, false)]
    [InlineData("Failing", 2000d, 3000d, false)]
    public void ImpossibleTrigger(string name, double? start, double? end, bool expected)
    {
        Assert.Equal(
            expected,
            StoryboardPatterns.IsImpossibleTrigger(
                new StoryboardTrigger(name, start, end),
                [Map([], TestHitObjects.Circle(1000))]
            )
        );
    }

    [Fact]
    public void Storyboards_AreThoseWithSpritesOrAnimations()
    {
        const string sprite = "Sprite,Pass,Centre,\"a.png\",320,240";
        const string animation = "Animation,Pass,Centre,\"a.png\",320,240,2,100,LoopForever";

        int Count(string[] events, params string[] osb)
        {
            using var context = CheckTestContext.CreateFromOsu(
                new OsuBuilder().WithDefaultTiming().WithDefaultHitObject().Events(events),
                extraFileContents: osb.Length == 0
                    ? null
                    :
                    [
                        (
                            "MapsetVerifier - MapsetVerifier (Tests).osb",
                            "[Events]\n" + string.Join("\n", osb)
                        ),
                    ]
            );

            return StoryboardPatterns.GetStoryboards(context.BeatmapSet).Count();
        }

        Assert.Equal(0, Count([]));
        Assert.Equal(1, Count([sprite]));
        Assert.Equal(1, Count([animation]));
        Assert.Equal(1, Count([], sprite));
        Assert.Equal(1, Count([], animation));
        Assert.Equal(2, Count([sprite], sprite));
        Assert.Equal(0, Count([], "Sample,0,0,\"a.wav\""));
    }

    [Theory]
    [InlineData(new[] { "F,0,0,100,0,1", "F,0,10,110,1,0", "F,0,20,120,0,1" }, 1)]
    [InlineData(new[] { "F,0,0,100,0,1", "F,0,0,110,1,0", "F,0,10,120,0,1" }, 0)] // At one time.
    [InlineData(new[] { "F,0,0,100,0,1", "F,0,500,600,1,0", "F,0,510,620,0,1" }, 0)] // Two by two.
    [InlineData(new[] { "F,0,0,100,0,1", "S,0,10,110,1,0", "R,0,20,120,0,1" }, 0)] // Of each one.
    public void DenseRuns(string[] commands, int expected)
    {
        Assert.Equal(expected, StoryboardPatterns.GetDenseRuns(Sprite(commands), 16, 2).Count());
    }

    [Theory]
    [InlineData(24)]
    [InlineData(31)]
    public void Easings_GoingPastTheirValues(int easing)
    {
        Assert.False(
            StoryboardPatterns.IsNeverOnScreen(
                Sprite(["F,0,0,1000,1", $"MX,{easing},0,1000,-600,-200"]),
                100,
                100
            )
        );
    }

    [Theory]
    [InlineData(23)]
    [InlineData(32)]
    public void Easings_StayingWithinTheirValues(int easing)
    {
        Assert.True(
            StoryboardPatterns.IsNeverOnScreen(
                Sprite(["F,0,0,1000,1", $"MX,{easing},0,1000,-600,-200"]),
                100,
                100
            )
        );
    }

    [Theory]
    [InlineData(500, true)] // Before the only object.
    [InlineData(1500, false)]
    [InlineData(2500, true)]
    public void HitSoundTriggers_NeedAnObject(int time, bool impossible)
    {
        var map = Map(
            [
                "Sprite,Pass,Centre,\"a.png\",320,240",
                $" T,HitSoundClap,{time},{time + 1000}",
                "  F,0,0,100,1",
            ],
            TestHitObjects.Circle(2000)
        );

        Assert.Equal(
            impossible,
            StoryboardPatterns.IsImpossibleTrigger(map.Sprites[0].triggers[0], [map])
        );
    }
}
