using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckHitSoundFormatTests
{
    [Fact]
    public void Wave_IsFine_ButOnlyNamedAsSuch()
    {
        var wave = TestMedia.Wave(100, TestMedia.Level(0.5));

        using (var context = HitSoundContext.Create(wave))
            Assert.Empty(context.RunGeneralCheck<CheckHitSoundFormat>());

        using (var context = HitSoundContext.Create(wave, "soft-hitnormal2.mp3"))
        {
            var issue = Assert.Single(context.RunGeneralCheck<CheckHitSoundFormat>());

            Assert.Equal(Issue.Level.Warning, issue.level);
            Assert.Contains("doesn't use the .wav or .ogg extension", issue.message);
        }
    }

    [Fact]
    public void BrokenFile_CouldNotBeChecked()
    {
        using var context = HitSoundContext.Create([1, 2, 3]);

        Assert.Equal(
            Issue.Level.Error,
            Assert.Single(context.RunGeneralCheck<CheckHitSoundFormat>()).level
        );
    }

    [Theory]
    [InlineData("soft-hitnormal2.mp3", true)]
    [InlineData("soft-hitclap2.mp3", false)]
    [InlineData("soft-sliderslide2.mp3", false)] // Not clicked.
    public void Mp3_IsAProblem_OnlyOnWhatIsClicked(string name, bool expected)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .TimingPoints(TestTimingPoints.Uninherited(0, 500, sampleIndex: 2))
                        .HitObjects(
                            TestHitObjects.Spinner(0, 500),
                            TestHitObjects.Slider(1000),
                            TestHitObjects.Circle(2000)
                        )
                        .Build()
                ),
            ],
            extraBinaryFiles: [(name, TestMedia.Mp3())]
        );

        var issues = context.RunGeneralCheck<CheckHitSoundFormat>();

        Assert.Equal(expected, issues.Count > 0);
        Assert.All(issues, issue => Assert.Contains("00:01:000", issue.message));
    }

    [Theory]
    [InlineData("soft-hitnormal2.ogg", null)]
    [InlineData("soft-hitnormal2.wav", null)]
    [InlineData("soft-hitnormal2.mp3", "doesn't use the .wav or .ogg extension")]
    public void Ogg_IsFine_IfNamedAsSuch(string name, string? expected)
    {
        using var context = HitSoundContext.Create(TestMedia.Ogg(199), name);

        var issue = context.RunGeneralCheck<CheckHitSoundFormat>().SingleOrDefault();

        if (expected == null)
            Assert.Null(issue);
        else
            Assert.Contains(expected, issue!.message);
    }

    [Fact]
    public void OtherFormats_AreUnexpected()
    {
        using var context = HitSoundContext.Create(TestMedia.Aiff(4410));

        var issue = Assert.Single(context.RunGeneralCheck<CheckHitSoundFormat>());

        Assert.Contains("soft-hitnormal2.wav", issue.message);
        Assert.Contains("AIFF", issue.message);
    }
}
