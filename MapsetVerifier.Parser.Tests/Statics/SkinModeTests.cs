using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Statics;

/// <summary> Which modes and objects a skin element needs to have any effect. </summary>
public sealed class SkinModeTests : IDisposable
{
    private readonly string folder = Path.Combine(
        Path.GetTempPath(),
        "MapsetVerifierParserTests",
        Guid.NewGuid().ToString("N")
    );

    public SkinModeTests() => Directory.CreateDirectory(folder);

    public void Dispose()
    {
        try
        {
            Directory.Delete(folder, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private const string Circle = "256,192,1000,1,0,0:0:0:0:";
    private const string Slider = "0,100,1000,2,0,L|280:100,1,140,0|0,0:0|0:0,0:0:0:0:";
    private const string ReverseSlider =
        "0,100,1000,2,0,L|280:100,2,140,0|0|0,0:0|0:0|0:0,0:0:0:0:";
    private const string Spinner = "256,192,1000,12,0,3000,0:0:0:0:";

    private bool IsUsed(string element, int mode, string hitObject = Circle, string? file = null)
    {
        File.WriteAllText(Path.Combine(folder, "a.osu"), TestBeatmap.Code([hitObject], mode: mode));
        if (file != null)
            File.WriteAllText(Path.Combine(folder, file), "");

        return SkinStatic.IsUsed(element, new BeatmapSet(folder));
    }

    // Standard can be converted into any other mode.
    [Theory]
    [InlineData("hit0-0.png", true, false, false, false)]
    [InlineData("selection-mod-fadein.png", true, false, false, true)]
    [InlineData("pippidonclear.png", true, true, false, false)]
    public void ModeElements(string element, bool standard, bool taiko, bool catcher, bool mania)
    {
        Assert.Equal(standard, IsUsed(element, 0));
        Assert.Equal(taiko, IsUsed(element, 1));
        Assert.Equal(catcher, IsUsed(element, 2));
        Assert.Equal(mania, IsUsed(element, 3));
    }

    [Theory]
    [InlineData("sliderstartcircle.png", 0, Slider, true)]
    [InlineData("sliderstartcircle.png", 0, Circle, false)]
    [InlineData("sliderstartcircle.png", 2, Slider, false)]
    [InlineData("taiko-roll-middle.png", 1, Slider, true)]
    [InlineData("taiko-roll-middle.png", 0, Slider, true)]
    [InlineData("taiko-roll-middle.png", 1, Circle, false)]
    [InlineData("taiko-roll-middle.png", 2, Slider, false)]
    [InlineData("spinner-warning.png", 1, Spinner, true)]
    [InlineData("spinner-warning.png", 1, Circle, false)]
    [InlineData("spinner-warning.png", 2, Spinner, false)]
    [InlineData("reversearrow.png", 0, ReverseSlider, true)]
    [InlineData("reversearrow.png", 0, Slider, false)]
    public void ObjectElements(string element, int mode, string hitObject, bool expected)
    {
        Assert.Equal(expected, IsUsed(element, mode, hitObject));
    }

    [Theory]
    [InlineData("particle50.png", "hit50.png")]
    [InlineData("particle100.png", "hit100.png")]
    [InlineData("particle300.png", "hit300.png")]
    public void Particles_NeedTheirHitBurst(string element, string file)
    {
        Assert.False(IsUsed(element, 0));
        Assert.True(IsUsed(element, 0, file: file));
    }

    [Fact]
    public void Spinner_WithAnotherBeatmapsObjects_DoesNotMatter()
    {
        Assert.False(IsUsed("spinner-approachcircle.png", 0, Circle));
        Assert.True(IsUsed("spinner-approachcircle.png", 0, Spinner));
        Assert.False(IsUsed("spinner-approachcircle.png", 2, Spinner));
    }
}
