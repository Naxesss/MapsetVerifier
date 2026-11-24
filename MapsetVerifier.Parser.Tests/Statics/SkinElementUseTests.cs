using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Statics;

/// <summary>Which modes and objects make a skin element used.</summary>
public sealed class SkinElementUseTests : IDisposable
{
    private const string Circle = "256,192,1000,1,0,0:0:0:0:";
    private const string Slider = "0,100,1000,2,0,L|280:100,1,140";
    private const string Reverse = "0,100,1000,2,0,L|280:100,2,140";
    private const string Spinner = "256,192,1000,12,0,2000,0:0:0:0:";

    private readonly string folder = Directory
        .CreateTempSubdirectory("MapsetVerifierParserTests")
        .FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private BeatmapSet Set(int mode, string hitObject, params string[] files)
    {
        File.WriteAllText(
            Path.Combine(folder, "map.osu"),
            TestBeatmap.Code([hitObject], mode: mode, general: "Countdown: 0")
        );

        foreach (var file in files)
            File.WriteAllText(Path.Combine(folder, file), "");

        return new BeatmapSet(folder);
    }

    [Theory]
    // Modes standard converts to are skinned by standard too.
    [InlineData("fruit-catcher-idle.png", "1010")]
    [InlineData("taikohitcircle.png", "1100")]
    [InlineData("selection-mod-key1.png", "1001")]
    [InlineData("hitcircle.png", "1000")]
    [InlineData("scorebar-ki.png", "1110")]
    public void PerMode(string element, string used)
    {
        for (var mode = 0; mode < 4; mode++)
            Assert.Equal(used[mode] == '1', SkinStatic.IsUsed(element, Set(mode, Circle)));
    }

    [Theory]
    [InlineData("sliderstartcircle.png", Slider, "1000")]
    [InlineData("taiko-roll-end.png", Slider, "1100")]
    [InlineData("fruit-drop.png", Slider, "1010")]
    [InlineData("spinner-rpm.png", Spinner, "1000")]
    [InlineData("spinner-warning.png", Spinner, "1100")]
    [InlineData("fruit-bananas.png", Spinner, "1010")]
    public void PerModeAndObject(string element, string hitObject, string used)
    {
        for (var mode = 0; mode < 4; mode++)
        {
            Assert.Equal(used[mode] == '1', SkinStatic.IsUsed(element, Set(mode, hitObject)));
            Assert.False(SkinStatic.IsUsed(element, Set(mode, Circle)));
        }
    }

    [Theory]
    [InlineData(Circle, false)]
    [InlineData(Slider, false)]
    [InlineData(Reverse, true)]
    public void ReverseArrow_NeedsAReverse(string hitObject, bool expected)
    {
        Assert.Equal(expected, SkinStatic.IsUsed("reversearrow.png", Set(0, hitObject)));
    }

    [Theory]
    [InlineData("particle50.png", "hit50.png", true)]
    [InlineData("particle50.png", "hit100.png", false)]
    [InlineData("particle100.png", "hit100.png", true)]
    [InlineData("particle300.png", "hit300.png", true)]
    // Frames of an animation take the place of the still image.
    [InlineData("hit100.png", "hit100-0.png", false)]
    [InlineData("hit100.png", "hit100-x.png", true)]
    [InlineData("hit100.png", "hit100-0x.png", true)]
    [InlineData("hit100.png", "a.png", true)]
    // Frames are numbered.
    [InlineData("hit100-12.png", "a.png", true)]
    [InlineData("hit100-a.png", "a.png", false)]
    [InlineData("hit100-1", "a.png", false)]
    [InlineData("unknown.png", "a.png", false)]
    public void DependingOnOtherFiles(string element, string file, bool expected)
    {
        Assert.Equal(expected, SkinStatic.IsUsed(element, Set(0, Circle, file)));
    }

    [Theory]
    [InlineData("rank-up.wav", Circle, "1111")]
    [InlineData("score-pp.png", Circle, "1111")]
    [InlineData("sliderendmiss-3.png", Circle, "1000")]
    [InlineData("spinnerbonus-max.wav", Spinner, "1000")]
    [InlineData("spinnerbonus-max.wav", Circle, "0000")]
    [InlineData("hitcircle.png", Circle, "0000")]
    public void LazerOnly(string element, string hitObject, string used)
    {
        for (var mode = 0; mode < 4; mode++)
            Assert.Equal(used[mode] == '1', SkinStatic.IsLazerOnly(element, Set(mode, hitObject)));
    }

    /// <summary> One difficulty or object which uses an element is enough. </summary>
    [Fact]
    public void OneUse_IsEnough()
    {
        File.WriteAllText(
            Path.Combine(folder, "a.osu"),
            TestBeatmap.Code([Circle, "256,192,9000,12,0,9500,0:0:0:0:"], events: ["2,2000,8000"])
        );
        File.WriteAllText(
            Path.Combine(folder, "b.osu"),
            TestBeatmap.Code(
                [Circle, "0,100,5000,2,0,L|280:100,2,140"],
                metadata: "Title:Title\nArtist:Artist\nCreator:Creator\nVersion:Other"
            )
        );
        var set = new BeatmapSet(folder);

        foreach (
            var element in new[]
            {
                "section-pass.png",
                "spinner-rpm.png",
                "spinner-warning.png",
                "fruit-bananas.png",
                "sliderstartcircle.png",
                "taiko-roll-end.png",
                "fruit-drop.png",
                "reversearrow.png",
            }
        )
            Assert.True(SkinStatic.IsUsed(element, set), element);
    }

    [Fact]
    public void OneDifficultyOfAMode_IsEnough()
    {
        foreach (var mode in new[] { 1, 2, 3 })
            File.WriteAllText(
                Path.Combine(folder, $"{mode}.osu"),
                TestBeatmap.Code(
                    [Circle],
                    mode: mode,
                    metadata: $"Title:Title\nArtist:Artist\nCreator:Creator\nVersion:{mode}"
                )
            );
        var set = new BeatmapSet(folder);

        Assert.True(SkinStatic.IsUsed("selection-mod-key1.png", set));
        Assert.True(SkinStatic.IsUsed("taikohitcircle.png", set));
        Assert.True(SkinStatic.IsUsed("fruit-catcher-idle.png", set));
        Assert.True(SkinStatic.IsUsed("scorebar-ki.png", set));
        Assert.False(SkinStatic.IsUsed("hitcircle.png", set));
    }
}
