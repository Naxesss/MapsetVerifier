using MapsetVerifier.Checks.AllModes.Events;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Events;

public class CheckStoryHitSoundsTests
{
    [Fact]
    public void SamplesOfDifficultiesAndTheStoryboard_AreWarnedAbout()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Artist("Artist")
                        .Title("Title")
                        .Creator("Creator")
                        .Events("Sample,1500,0,\"a.wav\",60")
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFileContents:
            [
                ("Artist - Title (Creator).osb", "[Events]\nSample,2500,0,\"b.wav\",70"),
            ]
        );

        var issues = context.RunBeatmapSetCheck<CheckStoryHitSounds>();

        Assert.Equal(
            [
                "00:01:500 - Storyboarded hit sound (a.wav, 60%) from .osu file.",
                "00:02:500 - Storyboarded hit sound (b.wav, 70%) from .osb file.",
            ],
            issues.Select(issue => issue.message)
        );
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
    }
}
