using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Statics;

public class ParserStaticTests
{
    private static List<string> Section(string section, params string[] lines) =>
        ParserStatic.ParseSection(lines, section, line => line).ToList();

    [Fact]
    public void ParseSection_ReturnsLinesUntilTheNextSection() =>
        Assert.Equal(
            ["a", "b"],
            Section("Events", "[General]", "x", "[Events]", "a", "b", "[Next]", "y")
        );

    [Fact]
    public void ParseSection_SkipsBlankLinesAndStripsCarriageReturns() =>
        Assert.Equal(["a", "b"], Section("Events", "[Events]\r", "a\r", "  \r", "", "\t", "b\r"));

    [Fact]
    public void ParseSection_CombinesRepeatedSections() =>
        Assert.Equal(
            ["a", "b"],
            Section("Events", "[Events]", "a", "[Other]", "x", "[Events]", "b")
        );

    [Fact]
    public void ParseSection_FindsTheHeaderAnywhereInALine() =>
        Assert.Equal(["a"], Section("Events", "x[[Events]]]]", "a"));

    [Fact]
    public void Events_AreFoundByNameAndById()
    {
        const string code = """
            osu file format v14

            [Events]
            //Background and Video events
            0,0,"id.jpg",0,0
            Background,0,"name.jpg",0,0
            Video,500,"id.mp4"
            1,600,"name.mp4"
            2,100,200
            Break,300,400
            01,0,"not an event type.jpg",0,0

            [TimingPoints]
            0,500,4,2,0,100,1,0

            [HitObjects]
            """;

        var beatmap = new Beatmap(code, "song", "map.osu");

        Assert.Equal(["id.jpg", "name.jpg"], beatmap.Backgrounds.Select(b => b.path));
        Assert.Equal(2, beatmap.Videos.Count);
        Assert.Equal(2, beatmap.Breaks.Count);
    }
}
