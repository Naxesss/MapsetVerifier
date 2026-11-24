using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Statics;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Statics;

public class StoryboardStaticTests
{
    private const string SpriteLine = "Sprite,Foreground,Centre,\"sb/a.png\",320,240";

    private static Sprite ParseSprite(params string[] commands) =>
        Assert.Single(StoryboardStatic.ParseStoryboard([SpriteLine, .. commands]).Sprites);

    [Theory]
    [InlineData(" F,0,0,100,1,0.5", StoryboardCommand.Kind.Fade, 1, 0, 0.5, 0)]
    [InlineData("_F,0,0,100,1,0.5", StoryboardCommand.Kind.Fade, 1, 0, 0.5, 0)]
    [InlineData(" M,0,0,100,1,2,3,4", StoryboardCommand.Kind.Move, 1, 2, 3, 4)]
    [InlineData(" MX,0,0,100,1,2", StoryboardCommand.Kind.MoveX, 1, 0, 2, 0)]
    [InlineData(" MY,0,0,100,1,2", StoryboardCommand.Kind.MoveY, 1, 0, 2, 0)]
    [InlineData(" S,0,0,100,1,2", StoryboardCommand.Kind.Scale, 1, 0, 2, 0)]
    [InlineData(" V,0,0,100,1,2,3,4", StoryboardCommand.Kind.VectorScale, 1, 2, 3, 4)]
    [InlineData(" R,0,0,100,1,2", StoryboardCommand.Kind.Rotate, 1, 0, 2, 0)]
    [InlineData(" C,0,0,100,1,2,0,3,4,0", StoryboardCommand.Kind.Colour, 1, 2, 3, 4)]
    public void Command_IsParsed(
        string line,
        StoryboardCommand.Kind kind,
        float startX,
        float startY,
        float endX,
        float endY
    ) =>
        Assert.Equal(
            new StoryboardCommand(
                kind,
                0,
                0,
                100,
                new Vector3(startX, startY, 0),
                new Vector3(endX, endY, 0)
            ),
            Assert.Single(ParseSprite(line).commands)
        );

    [Fact]
    public void Parameter_IsParsed()
    {
        var command = Assert.Single(ParseSprite(" P,0,0,100,A").commands);

        Assert.Equal(StoryboardCommand.Kind.Parameter, command.Type);
        Assert.Equal('A', command.Parameter);
    }

    [Fact]
    public void MissingEndTimeAndValue_UseTheStart()
    {
        var command = Assert.Single(ParseSprite(" M,0,500,,10,20").commands);

        Assert.Equal(500, command.EndTime);
        Assert.Equal(new Vector3(10, 20, 0), command.EndValue);
    }

    [Fact]
    public void MoreThanTwoValues_AreSplitIntoASequence()
    {
        var commands = ParseSprite(" F,0,0,100,0,1,0").commands;

        Assert.Equal([(0, 100), (100, 200)], commands.Select(c => (c.StartTime, c.EndTime)));
        Assert.Equal([(0, 1), (1, 0)], commands.Select(c => (c.StartValue.X, c.EndValue.X)));
    }

    [Fact]
    public void LoopsAndTriggers_GroupWhatIsNestedBelowThem()
    {
        var sprite = ParseSprite(
            " L,1000,3",
            "  F,0,0,100,0,1",
            "// comment",
            "__F,0,100,200,1,0",
            " T,HitSoundClap,2000,3000",
            "  S,0,0,100,1,2",
            " R,0,0,100,0,1",
            " T,Failing",
            "  S,0,0,100,1,2"
        );

        Assert.Equal(StoryboardCommand.Kind.Rotate, Assert.Single(sprite.commands).Type);

        var loop = Assert.Single(sprite.commandLoops);
        Assert.Equal(2, loop.commands.Count);
        Assert.Equal(3, loop.count);
        Assert.Equal(1600, loop.GetEndTime());

        Assert.Equal(
            [("HitSoundClap", 2000d, 3000d), ("Failing", (double?)null, (double?)null)],
            sprite.triggers.Select(trigger => (trigger.name, trigger.startTime, trigger.endTime))
        );
        Assert.All(sprite.triggers, trigger => Assert.Single(trigger.commands));
    }

    [Fact]
    public void Loop_StartsWithItsFirstCommand()
    {
        var loop = Assert.Single(ParseSprite(" L,1000,2", "  F,0,50,150,0,1").commandLoops);

        Assert.Equal(1050, loop.GetStartTime());
        Assert.Equal(100, loop.GetIterationDuration());
        Assert.Equal(1250, loop.GetEndTime());
    }

    [Fact]
    public void ActiveTime_IncludesLoopsButNotTriggers()
    {
        var sprite = ParseSprite(
            " F,0,500,600,0,1",
            " L,1000,3",
            "  F,0,0,100,0,1",
            " T,HitSound,0,9000",
            "  F,0,0,100,0,1"
        );

        Assert.Equal((500, 1300), sprite.GetActiveTime());
        Assert.Null(ParseSprite(" T,HitSound,0,9000", "  F,0,0,100,0,1").GetActiveTime());
    }

    [Theory]
    [InlineData(" X,0,0,100,1")]
    [InlineData(" F,0")]
    [InlineData(" F,0,a,100,1")]
    [InlineData(" F,0,0,a,1")]
    [InlineData(" F,0,0,100,a")]
    [InlineData(" M,0,0,100,1")]
    [InlineData(" P,0,0,100,")]
    [InlineData(" ")]
    public void UnreadableCommand_IsLeftOut(string line) =>
        Assert.Empty(ParseSprite(line).commands);

    [Fact]
    public void CommandsBelowAnUnreadableLoop_AreLeftOut()
    {
        var sprite = ParseSprite(" L,a,3", "  F,0,0,100,0,1", " F,0,0,100,1");

        Assert.Empty(sprite.commandLoops);
        Assert.Single(sprite.commands);
    }

    [Fact]
    public void CommandsBelowOtherEvents_AreIgnored()
    {
        var (sprites, animations) = StoryboardStatic.ParseStoryboard([
            SpriteLine,
            " F,0,0,100,1",
            "0,0,\"bg.jpg\",0,0",
            " F,0,0,100,1",
            "Animation,Fail,Centre,\"sb/b.png\",320,280,2,40,LoopForever",
            " F,0,0,100,1",
            " F,0,100,200,1",
        ]);

        Assert.Single(Assert.Single(sprites).commands);
        Assert.Equal(2, Assert.Single(animations).commands.Count);
    }

    [Fact]
    public void Osb_ReplacesVariablesInCommands()
    {
        var osb = new Osb(
            $"[Variables]\r\n$white=255,255,255\r\n[Events]\r\n{SpriteLine}\r\n C,0,0,100,$white\r\n"
        );

        Assert.Equal(
            new Vector3(255),
            Assert.Single(Assert.Single(osb.sprites).commands).StartValue
        );
    }

    [Theory]
    [InlineData(" P,0,0,100,H", 1)]
    [InlineData(" P,0,0,100, A ", 1)]
    [InlineData(" P,0,0,100,", 0)]
    [InlineData(" P,0,0,100,HV", 0)]
    public void Parameter_IsASingleLetter(string line, int expected)
    {
        Assert.Equal(expected, ParseSprite(line).commands.Count);
    }
}
