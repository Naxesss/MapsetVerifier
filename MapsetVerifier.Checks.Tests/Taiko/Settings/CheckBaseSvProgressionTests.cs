using MapsetVerifier.Checks.Taiko.Settings;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Taiko.Settings;

public class CheckBaseSvProgressionTests
{
    [Fact]
    public void HighBpmAscending_ProducesNoIssues()
    {
        var issues = RunCheck(
            ("Kantan", 1.1f),
            ("Futsuu", 1.2f),
            ("Muzukashii", 1.3f),
            ("Oni", 1.4f),
            ("Inner Oni", 1.4f)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void DoubleBpmAscending_ProducesNoIssues()
    {
        var issues = RunCheck(
            ("Kantan", 1.4f),
            ("Futsuu", 1.4f),
            ("Muzukashii", 1.4f),
            ("Oni", 1.4f),
            ("Inner Oni", 2.4f),
            ("Hell Oni", 2.8f)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void LowSvDescending_ProducesNoIssues()
    {
        var issues = RunCheck(
            ("Kantan", 1.4f),
            ("Futsuu", 1.4f),
            ("Muzukashii", 1.2f),
            ("Oni", 1.1f),
            ("Inner Oni", 0.9f)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void AllEqual_ProducesNoIssues()
    {
        var issues = RunCheck(
            ("Kantan", 1.4f),
            ("Futsuu", 1.4f),
            ("Muzukashii", 1.4f),
            ("Oni", 1.4f),
            ("Inner Oni", 1.4f)
        );

        Assert.Empty(issues);
    }

    [Fact]
    public void DipThenRise_FlagsEarlyDropOnAscendingDirection()
    {
        // Ascending has 1 violation (1.4→1.1); descending has 2 → prefer ascending → flag Futsuu.
        var issues = RunCheck(
            ("Kantan", 1.4f),
            ("Futsuu", 1.1f),
            ("Muzukashii", 1.2f),
            ("Oni", 1.4f)
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("Futsuu", issue.beatmap?.MetadataSettings.version);
        Assert.Contains(
            "Base SV 1.10x breaks the set's progression from 1.40x on Kantan",
            issue.message
        );
        Assert.Contains("Ensure this makes sense", issue.message);
    }

    [Fact]
    public void SpikeThenDrop_FlagsFinalDropOnAscendingTieBreak()
    {
        // One ascending + one descending violation → ascending wins on tie → flag final drop.
        var issues = RunCheck(
            ("Kantan", 1.4f),
            ("Futsuu", 1.4f),
            ("Muzukashii", 1.4f),
            ("Oni", 1.6f),
            ("Inner Oni", 1.4f)
        );

        var issue = Assert.Single(issues);
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Equal("Inner Oni", issue.beatmap?.MetadataSettings.version);
        Assert.Contains(
            "Base SV 1.40x breaks the set's progression from 1.60x on Oni",
            issue.message
        );
    }

    [Fact]
    public void FewerThanThreeTaikoDiffs_ProducesNoIssues()
    {
        Assert.Empty(RunCheck(("Kantan", 1.4f)));
        Assert.Empty(RunCheck(("Kantan", 1.4f), ("Futsuu", 0.9f)));
    }

    [Fact]
    public void MixedMode_IgnoresNonTaikoDiffs()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("kantan.osu", BuildTaikoOsu("Kantan", 1.4f)),
            ("futsuu.osu", BuildTaikoOsu("Futsuu", 1.1f)),
            ("muzukashii.osu", BuildTaikoOsu("Muzukashii", 1.2f)),
            ("oni.osu", BuildTaikoOsu("Oni", 1.4f)),
            // Would break progression if included, but Standard must be ignored.
            ("standard.osu", BuildOsu(Beatmap.Mode.Standard, "Hard", 0.5f)),
        ]);

        var issues = context.RunBeatmapSetCheck<CheckBaseSvProgression>();

        var issue = Assert.Single(issues);
        Assert.Equal("Futsuu", issue.beatmap?.MetadataSettings.version);
    }

    private static List<Issue> RunCheck(params (string Version, float SliderMultiplier)[] diffs)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            diffs
                .Select(diff =>
                    (
                        $"{diff.Version.Replace(' ', '_')}.osu",
                        BuildTaikoOsu(diff.Version, diff.SliderMultiplier)
                    )
                )
                .ToArray()
        );

        return context.RunBeatmapSetCheck<CheckBaseSvProgression>();
    }

    private static string BuildTaikoOsu(string version, float sliderMultiplier) =>
        BuildOsu(Beatmap.Mode.Taiko, version, sliderMultiplier);

    private static string BuildOsu(Beatmap.Mode mode, string version, float sliderMultiplier) =>
        string.Join(
            "\n",
            "osu file format v14",
            "[General]",
            "AudioFilename:",
            $"Mode: {(int)mode}",
            "[Metadata]",
            "Title:Base SV Progression",
            "Artist:Tests",
            "Creator:Tests",
            $"Version:{version}",
            "[Difficulty]",
            "CircleSize:5",
            "HPDrainRate:5",
            "OverallDifficulty:5",
            "ApproachRate:5",
            $"SliderMultiplier:{sliderMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            "SliderTickRate:1",
            "[Events]",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            "256,192,1000,1,0,0:0:0:0:"
        );
}
