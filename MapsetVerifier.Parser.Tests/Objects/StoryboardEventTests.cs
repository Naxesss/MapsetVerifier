using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

/// <summary>Events of difficulties (.osu) and of storyboards (.osb).</summary>
public class StoryboardEventTests
{
    private static Beatmap Events(params string[] lines) =>
        TestBeatmap.Parse(TestBeatmap.Code(events: lines));

    [Theory]
    [InlineData("0,0,\"Folder\\BG.jpg\",10,-20")]
    [InlineData("Background,0,\"Folder\\BG.jpg\",10,-20")]
    public void Background_ById_OrByName(string line)
    {
        var background = Assert.Single(Events(line).Backgrounds);

        Assert.Equal("Folder/BG.jpg", background.path);
        Assert.Equal("folder/bg", background.strippedPath);
        Assert.Equal(new Vector2(10, -20), background.offset);
    }

    [Fact]
    public void Background_WithoutOffset()
    {
        Assert.Null(Assert.Single(Events("0,0,\"bg.jpg\"").Backgrounds).offset);
    }

    [Theory]
    [InlineData("1,-250,\"video.mp4\"")]
    [InlineData("Video,-250,\"video.mp4\"")]
    public void Video_ById_OrByName(string line)
    {
        var video = Assert.Single(Events(line).Videos);

        Assert.Equal(-250, video.offset);
        Assert.Equal("video.mp4", video.path);
    }

    [Theory]
    [InlineData("2,1000,5000")]
    [InlineData("Break,1000,5000")]
    public void Break_ById_OrByName(string line)
    {
        var @break = Assert.Single(Events(line).Breaks);

        Assert.Equal(1000, @break.time);
        Assert.Equal(5000, @break.endTime);
    }

    [Fact]
    public void Break_ReachesFromTheObjectBeforeToTheOneAfter()
    {
        var beatmap = TestBeatmap.Parse(
            TestBeatmap.Code(
                ["256,192,1000,12,0,2000,0:0:0:0:", TestBeatmap.Circle(9000)],
                events: ["2,2200,8000"]
            )
        );
        var @break = beatmap.Breaks[0];

        Assert.Equal(2000, @break.GetRealStart(beatmap));
        Assert.Equal(9000, @break.GetRealEnd(beatmap));
        Assert.Equal(7000, @break.GetDuration(beatmap));
    }

    [Fact]
    public void Sprite_AllFields()
    {
        var sprite = Assert.Single(
            Events("Sprite,Foreground,TopLeft,\"SB\\Star.png\",320.5,-240").Sprites
        );

        Assert.Equal(Sprite.Layer.Foreground, sprite.layer);
        Assert.Equal(Sprite.Origin.TopLeft, sprite.origin);
        Assert.Equal("SB/Star.png", sprite.path);
        Assert.Equal("sb/star", sprite.strippedPath);
        Assert.Equal(new Vector2(320.5f, -240), sprite.offset);
    }

    [Fact]
    public void Sprite_WithAnUnknownLayerOrOrigin_IsMarkedAsSuch()
    {
        var sprite = Assert.Single(Events("Sprite,Nowhere,Middle,\"a.png\",0,0").Sprites);

        Assert.Equal(Sprite.Layer.Unknown, sprite.layer);
        Assert.Equal(Sprite.Origin.Unknown, sprite.origin);
    }

    [Fact]
    public void Commands_AreNotEvents()
    {
        var beatmap = Events(
            "Sprite,Foreground,Centre,\"a.png\",320,240",
            " F,0,0,500,0,1",
            " L,0,4",
            "  M,0,0,500,0,0,10,10"
        );

        Assert.Single(beatmap.Sprites);
        Assert.Empty(beatmap.Backgrounds);
    }

    [Fact]
    public void Animation_HasOneFileForEachFrame()
    {
        var animation = Assert.Single(
            Events("Animation,Fail,Centre,\"sb\\run.png\",320,240,3,62.5,LoopOnce").Animations
        );

        Assert.Equal(3, animation.frameCount);
        Assert.Equal(62.5, animation.frameDelay);
        Assert.False(animation.loops);
        Assert.Equal(["sb/run0.png", "sb/run1.png", "sb/run2.png"], animation.framePaths);
    }

    [Theory]
    [InlineData("Animation,Fail,Centre,\"a.png\",320,240,3,50,LoopForever")]
    public void Animation_LoopsForever(string line)
    {
        Assert.True(Assert.Single(Events(line).Animations).loops);
    }

    [Fact]
    public void Animation_WithoutALoopType_LoopsForever()
    {
        // The loop type is optional, and LoopForever by default.
        var animation = Assert.Single(
            Events("Animation,Fail,Centre,\"a.png\",320,240,3,50").Animations
        );

        Assert.True(animation.loops);
    }

    [Fact]
    public void Animation_WithoutAnExtension_IsParsed()
    {
        var animation = Assert.Single(
            Events("Animation,Fail,Centre,\"run\",320,240,2,50,LoopForever").Animations
        );

        Assert.Equal(2, animation.framePaths.Count);
    }

    [Theory]
    [InlineData("Sample,1500,Pass,\"SB\\Hit.wav\",60", Sample.Layer.Pass, 60)]
    [InlineData("5,1500,3,\"SB\\Hit.wav\",60", Sample.Layer.Foreground, 60)]
    [InlineData("Sample,1500,0,\"SB\\Hit.wav\"", Sample.Layer.Background, 100)]
    [InlineData("Sample,1500,9,\"SB\\Hit.wav\",60", Sample.Layer.Unknown, 60)]
    [InlineData("Sample,1500,Nowhere,\"SB\\Hit.wav\",60", Sample.Layer.Unknown, 60)]
    public void Sample_AllFields(string line, Sample.Layer layer, float volume)
    {
        var sample = Assert.Single(Events(line).Samples);

        Assert.Equal(1500, sample.time);
        Assert.Equal(layer, sample.layer);
        Assert.Equal("SB/Hit.wav", sample.path);
        Assert.Equal("sb/hit", sample.strippedPath);
        Assert.Equal(volume, sample.volume);
    }

    [Fact]
    public void Comments_AreIgnored()
    {
        var beatmap = Events("//Background and Video events", "//0,0,\"bg.jpg\"", "0,0,\"a.jpg\"");

        Assert.Equal("a.jpg", Assert.Single(beatmap.Backgrounds).path);
    }

    [Fact]
    public void Osb_AllEventTypes_AreParsed()
    {
        var osb = new Osb(
            """
            [Events]
            //Background and Video events
            0,0,"bg.jpg",0,0
            Video,100,"video.mp4"
            Sprite,Background,Centre,"a.png",320,240
             F,0,0,500,1
            Animation,Background,Centre,"b.png",320,240,2,100,LoopForever
            Sample,1000,0,"c.wav",70
            2,100,200
            """
        );

        Assert.Equal("bg.jpg", Assert.Single(osb.backgrounds).path);
        Assert.Equal("video.mp4", Assert.Single(osb.videos).path);
        Assert.Equal("a.png", Assert.Single(osb.sprites).path);
        Assert.Equal("b.png", Assert.Single(osb.animations).path);
        Assert.Equal("c.wav", Assert.Single(osb.samples).path);
        Assert.Single(osb.breaks);
        Assert.True(osb.IsUsed());
    }

    [Fact]
    public void Osb_Variables_AreSubstitutedInEvents()
    {
        var osb = new Osb(
            """
            [Variables]
            $star="sb\star.png"
            $pos=10,20

            [Events]
            Sprite,Foreground,Centre,$star,$pos
            """
        );
        var sprite = Assert.Single(osb.sprites);

        Assert.Equal("sb/star.png", sprite.path);
        Assert.Equal(new Vector2(10, 20), sprite.offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[Events]\n//Background and Video events\n//Storyboard Layer 0 (Background)")]
    public void Osb_WithoutEvents_IsNotUsed(string code)
    {
        Assert.False(new Osb(code).IsUsed());
    }

    [Fact]
    public void Osb_WindowsLineEndings_DoNotEndUpInPaths()
    {
        var osb = new Osb("[Events]\r\nVideo,0,\"a.mp4\"\r\n");

        Assert.Equal("a.mp4", Assert.Single(osb.videos).path);
    }

    [Theory]
    [InlineData(" T", 0, null, null)]
    [InlineData(" T,HitSound", 1, null, null)]
    [InlineData(" T,HitSound,x,y", 1, null, null)]
    [InlineData(" T,HitSound,1000", 1, 1000d, null)]
    [InlineData(" T,HitSound,1000,2000", 1, 1000d, 2000d)]
    public void Trigger_MayLeaveOutItsTimes(string line, int count, double? start, double? end)
    {
        var triggers = TestBeatmap
            .Parse(TestBeatmap.Code(events: ["Sprite,Pass,Centre,\"a.png\",320,240", line]))
            .Sprites[0]
            .triggers;

        Assert.Equal(count, triggers.Count);
        Assert.Equal(start, triggers.FirstOrDefault()?.startTime);
        Assert.Equal(end, triggers.FirstOrDefault()?.endTime);
    }

    [Theory]
    [InlineData(" P,0,0,100,H", 1)]
    [InlineData(" P,0,0,100,", 0)]
    [InlineData(" P,0,0,100,HV", 0)]
    public void Parameter_IsOneLetter(string line, int count)
    {
        Assert.Equal(
            count,
            TestBeatmap
                .Parse(TestBeatmap.Code(events: ["Sprite,Pass,Centre,\"a.png\",320,240", line]))
                .Sprites[0]
                .commands.Count
        );
    }

    [Fact]
    public void Variables_WithoutAValue_AreIgnored()
    {
        var osb = new Osb(
            """
            [Variables]
            $broken
            not a variable
            $white=255,255,255
            $path="a=b.png"
            [Events]
            Sprite,Pass,Centre,$path,320,240
             C,0,0,1000,$white
            """
        );

        Assert.Equal("a=b.png", Assert.Single(osb.sprites).path);
    }

    [Theory]
    [InlineData("Sprite,Pass,Centre,\"a.png\"")]
    [InlineData("Sprite,Pass,Centre,\"a.png\",100")]
    [InlineData("Sprite,Pass,Centre,\"a.png\",100,200")]
    public void Sprites_MayLackTheirPosition(string line)
    {
        Assert.Equal("a.png", Assert.Single(new Osb("[Events]\n" + line).sprites).path);
    }
}
