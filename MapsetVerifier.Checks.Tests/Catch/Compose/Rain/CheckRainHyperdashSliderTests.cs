using MapsetVerifier.Checks.Catch.Compose.Rain;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose.Rain;

public class CheckRainHyperdashSliderTests
{
    [Theory]
    [InlineData(JuiceStream.JuiceStreamPart.PartKind.Drop, 1)]
    [InlineData(JuiceStream.JuiceStreamPart.PartKind.Repeat, 1)]
    [InlineData(JuiceStream.JuiceStreamPart.PartKind.Tail, 0)] // Fine in a Rain.
    public void HyperdashWithinASlider_IsAWarning(
        JuiceStream.JuiceStreamPart.PartKind kind,
        int expected
    )
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Slider(1000, "L", "380:100", 2, 280, 0, 100),
                    TestHitObjects.Circle(5000, 0)
                )
        );
        var slider = (JuiceStream)context.GetBeatmap("Test").HitObjects[0];
        foreach (var part in slider.Parts)
            part.MovementType = CatchMovementType.Walk;
        slider.Parts.First(part => part.Kind == kind).MovementType = CatchMovementType.Hyperdash;

        var issues = context.RunBeatmapCheck<CheckRainHyperdashSlider>("Test");

        Assert.Equal(expected, issues.Count);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
    }
}
