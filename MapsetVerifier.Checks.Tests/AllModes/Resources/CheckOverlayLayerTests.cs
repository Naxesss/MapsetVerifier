using MapsetVerifier.Checks.AllModes.General.Resources;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Resources;

public class CheckOverlayLayerTests
{
    [Fact]
    public void ElementsInTheOverlayLayer_AreWarnedAbout()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Artist("Artist")
                        .Title("Title")
                        .Creator("Creator")
                        .Events(
                            "Sprite,Overlay,Centre,\"a.png\",320,240",
                            "Sprite,Foreground,Centre,\"b.png\",320,240",
                            "Animation,Overlay,Centre,\"c.png\",320,240,2,100,LoopOnce"
                        )
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFileContents:
            [
                (
                    "Artist - Title (Creator).osb",
                    "[Events]\nSprite,Overlay,Centre,\"d.png\",0,0\nAnimation,Overlay,Centre,\"e.png\",0,0,2,100,LoopOnce"
                ),
            ]
        );

        Assert.Equal(
            ["a.png .osu", "c.png .osu", "d.png .osb", "e.png .osb"],
            context
                .RunGeneralCheck<CheckOverlayLayer>()
                .Select(issue =>
                    issue.message.Split('"')[1]
                    + (issue.message.Contains(".osb") ? " .osb" : " .osu")
                )
        );
    }
}
