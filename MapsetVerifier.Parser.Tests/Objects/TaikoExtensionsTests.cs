using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects;

public class TaikoExtensionsTests
{
    private static Beatmap Taiko(params string[] hitObjects) =>
        TestBeatmap.Parse(TestBeatmap.Code(hitObjects, mode: 1));

    [Theory]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(2, false, false)]
    [InlineData(8, false, false)]
    [InlineData(4, true, true)]
    [InlineData(12, false, true)]
    public void HitSoundsDecideTheNote(int hitSound, bool isDon, bool isFinisher)
    {
        var note = Taiko(TestBeatmap.Circle(1000, hitSound: hitSound)).HitObjects[0];

        Assert.Equal(isDon, note.IsDon());
        Assert.Equal(isFinisher, note.IsFinisher());
    }

    [Fact]
    public void Mono_IsTheSameColourAsBefore()
    {
        var notes = Taiko(
            TestBeatmap.Circle(1000),
            TestBeatmap.Circle(1250, hitSound: 4),
            TestBeatmap.Circle(1500, hitSound: 8),
            TestBeatmap.Circle(1750, hitSound: 2)
        ).HitObjects;

        Assert.Equal([false, true, false, true], notes.Select(note => note.IsMono()));
    }

    [Fact]
    public void Pattern_IsAStretchOfEvenlySpacedNotes()
    {
        // 1/4 triple, 1/1 gap, 1/4 triple.
        var notes = Taiko(
            TestBeatmap.Circle(1000),
            TestBeatmap.Circle(1125),
            TestBeatmap.Circle(1250),
            TestBeatmap.Circle(1750),
            TestBeatmap.Circle(1875),
            TestBeatmap.Circle(2000)
        ).HitObjects;

        Assert.Equal(
            [true, false, false, true, false, false],
            notes.Select(note => note.IsAtBeginningOfPattern())
        );
        Assert.Equal(
            [false, false, true, false, false, true],
            notes.Select(note => note.IsAtEndOfPattern())
        );
        Assert.Equal(
            [false, true, false, false, true, false],
            notes.Select(note => note.IsInMiddleOfPattern())
        );
        Assert.DoesNotContain(notes, note => note.IsNotInPattern());
    }

    [Fact]
    public void ALoneNoteBetweenOtherObjects_IsNotInAPattern()
    {
        var notes = Taiko(
            "256,192,1000,12,0,2000,0:0:0:0:",
            TestBeatmap.Circle(2500),
            "256,192,3000,12,0,4000,0:0:0:0:"
        ).HitObjects;

        Assert.True(notes[1].IsNotInPattern());
    }
}
