using MapsetVerifier.Checks.AllModes.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Settings;

public class CheckInconsistentSettingsTests
{
    private static List<Issue> Run(
        Action<OsuBuilder>? a = null,
        Action<OsuBuilder>? b = null,
        string[]? generalA = null,
        string[]? generalB = null,
        string? osb = null
    )
    {
        string Build(string version, Action<OsuBuilder>? configure, string[]? general)
        {
            var builder = new OsuBuilder()
                .Artist("Artist")
                .Title("Title")
                .Creator("Creator")
                .Version(version)
                .WithDefaultTiming()
                .HitObjects(TestHitObjects.Circle(20000));
            configure?.Invoke(builder);

            return OsuText.General(builder.Build(), general ?? []);
        }

        using var context = CheckTestContext.CreateFromOsuFiles(
            [("a.osu", Build("A", a, generalA)), ("b.osu", Build("B", b, generalB))],
            extraFileContents: osb == null ? null : [("Artist - Title (Creator).osb", osb)]
        );

        return context.RunBeatmapSetCheck<CheckInconsistentSettings>();
    }

    private const string Storyboard = "Sprite,Pass,Centre,\"a.png\",320,240";

    /// <summary> Each inconsistency is reported in both difficulties, pointing at one another. </summary>
    private static void AssertInconsistent(List<Issue> issues, Issue.Level level, string setting)
    {
        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.Equal(level, issue.level));
        Assert.All(issues, issue => Assert.Contains($"Inconsistent {setting} ", issue.message));
        Assert.Equal(
            ["A", "B"],
            issues.Select(issue => issue.beatmap!.MetadataSettings.version).Order()
        );
    }

    [Fact]
    public void SameSettings_AreFine()
    {
        Assert.Empty(Run());
    }

    [Fact]
    public void AudioLeadIn()
    {
        AssertInconsistent(
            Run(generalB: ["AudioLeadIn: 1000"]),
            Issue.Level.Warning,
            "audio lead-in"
        );
    }

    [Fact]
    public void SkinPreference()
    {
        AssertInconsistent(
            Run(generalB: ["SkinPreference:Default"]),
            Issue.Level.Warning,
            "skin preference"
        );
    }

    [Fact]
    public void TickRate_IsMinor_AndOnlyWithinAMode()
    {
        AssertInconsistent(
            Run(b: builder => builder.SliderTickRate(2)),
            Issue.Level.Minor,
            "slider tick rate"
        );
        Assert.Empty(Run(b: builder => builder.SliderTickRate(2).Mode(Beatmap.Mode.Catch)));
    }

    [Fact]
    public void Countdown_OnlyWhereItShows()
    {
        // Room for one in either difficulty, at 20 seconds.
        AssertInconsistent(
            Run(b: builder => builder.Countdown(2), a: builder => builder.Countdown(1)),
            Issue.Level.Warning,
            "countdown speed"
        );
        // Taiko and mania have none.
        Assert.Empty(
            Run(
                a: builder => builder.Mode(Beatmap.Mode.Taiko).Countdown(1),
                b: builder => builder.Mode(Beatmap.Mode.Taiko).Countdown(2)
            )
        );
        Assert.Empty(
            Run(
                a: builder => builder.Countdown(1),
                b: builder => builder.Mode(Beatmap.Mode.Catch).Countdown(2)
            )
        );
    }

    [Fact]
    public void Letterbox_OnlyMattersWithBreaks()
    {
        Assert.Empty(Run(generalB: ["LetterboxInBreaks: 1"]));
        AssertInconsistent(
            Run(
                builder => builder.Events("2,0,1000"),
                builder => builder.Events("2,0,1000"),
                generalB: ["LetterboxInBreaks: 1"]
            ),
            Issue.Level.Warning,
            "letterbox"
        );
    }

    [Theory]
    [InlineData("WidescreenStoryboard: 1", "widescreen support")]
    [InlineData("StoryFireInFront: 1", "storyboard in front of combo fire")]
    [InlineData("UseSkinSprites: 1", "usage of skin sprites in storyboard")]
    [InlineData("EpilepsyWarning: 1", "epilepsy warning")]
    public void StoryboardSettings_OnlyMatterWithAStoryboard(string line, string setting)
    {
        Assert.Empty(Run(generalB: [line]));
        AssertInconsistent(
            Run(generalB: [line], osb: "[Events]\n" + Storyboard),
            Issue.Level.Warning,
            setting
        );
        AssertInconsistent(
            Run(
                builder => builder.Events(Storyboard),
                builder => builder.Events(Storyboard),
                generalB: [line]
            ),
            Issue.Level.Warning,
            setting
        );
    }

    [Fact]
    public void EpilepsyWarning_AlsoMattersWithVideos()
    {
        AssertInconsistent(
            Run(
                builder => builder.Events("Video,0,\"a.mp4\""),
                builder => builder.Events("Video,0,\"a.mp4\""),
                generalB: ["EpilepsyWarning: 1"]
            ),
            Issue.Level.Warning,
            "epilepsy warning"
        );
    }

    [Fact]
    public void DifficultySpecificStoryboard_InOnlyOne()
    {
        AssertInconsistent(
            Run(b: builder => builder.Events(Storyboard)),
            Issue.Level.Warning,
            "difficulty-specific storyboard presence"
        );
    }

    [Fact]
    public void BeatmapSetId_IsAProblem()
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            new[] { ("A", "-1"), ("B", "123") }.Select(pair =>
                (
                    pair.Item1 + ".osu",
                    new OsuBuilder()
                        .Version(pair.Item1)
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                        .Replace("[Difficulty]", $"BeatmapSetID:{pair.Item2}\n[Difficulty]")
                )
            )
        );

        AssertInconsistent(
            context.RunBeatmapSetCheck<CheckInconsistentSettings>(),
            Issue.Level.Problem,
            "beatmapset id"
        );
    }
}
