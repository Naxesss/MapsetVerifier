using MapsetVerifier.Checks.AllModes.General.Audio;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Audio;

public class CheckAudioUsageTests
{
    private static List<Issue> Run(int[] lastObjects, string? eventLine = null)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            lastObjects.Select(
                (time, i) =>
                    (
                        $"{i}.osu",
                        new OsuBuilder()
                            .Version($"Diff {i}")
                            .Events(eventLine == null ? [] : [eventLine])
                            .WithDefaultTiming()
                            .HitObjects(TestHitObjects.Circle(time))
                            .Build()
                    )
            ),
            extraBinaryFiles: [("audio.mp3", TestMedia.Wave(1000, TestMedia.Level(0.5)))]
        );

        return context.RunGeneralCheck<CheckAudioUsage>();
    }

    [Theory]
    [InlineData(801, null)]
    [InlineData(750, "25% unused")]
    public void LessThanEightyPercent_IsAWarning(int lastObject, string? expected)
    {
        var issues = Run([lastObject]);

        Assert.Equal(expected != null, issues.Count == 1);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
        Assert.All(issues, issue => Assert.Contains(expected!, issue.message));
    }

    [Fact]
    public void LongestDifficulty_Counts()
    {
        Assert.Empty(Run([500, 900]));
    }

    [Theory]
    [InlineData("Video,0,\"a.mp4\"", true)]
    [InlineData("Sprite,Pass,Centre,\"a.png\",0,0", true)]
    [InlineData(null, false)]
    public void VideoOrStoryboard_MayUseTheOutro(string? line, bool expected)
    {
        Assert.Equal(
            expected,
            Assert.Single(Run([500], line)).message.Contains("video or storyboard")
        );
    }

    [Fact]
    public void StoryboardOfTheSet_CountsToo_AndNoObjectsUseNothing()
    {
        List<Issue> RunWith(string[] objects, string? osb, byte[] audio)
        {
            using var context = CheckTestContext.CreateFromOsuFiles(
                [("a.osu", new OsuBuilder().WithDefaultTiming().HitObjects(objects).Build())],
                extraFileContents: osb == null
                    ? null
                    : [("MapsetVerifier - MapsetVerifier (Tests).osb", osb)],
                extraBinaryFiles: [("audio.mp3", audio)]
            );

            return context.RunGeneralCheck<CheckAudioUsage>();
        }

        var audio = TestMedia.Wave(1000, TestMedia.Level(0.5));
        string[] objects = [TestHitObjects.Circle(100)];

        var without = RunWith(objects, null, audio);
        var empty = RunWith(objects, "[Events]\n", audio);
        var used = RunWith(objects, "[Events]\nSprite,Pass,Centre,\"a.png\",0,0", audio);

        Assert.Equal(Assert.Single(without).message, Assert.Single(empty).message);
        Assert.NotEqual(Assert.Single(without).message, Assert.Single(used).message);
        Assert.Contains("100% unused", Assert.Single(RunWith([], null, audio)).message);
        Assert.Equal(Issue.Level.Error, Assert.Single(RunWith(objects, null, [1, 2, 3])).level);
    }
}
