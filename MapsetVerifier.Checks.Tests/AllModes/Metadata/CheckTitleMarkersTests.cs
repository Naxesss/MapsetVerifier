using MapsetVerifier.Checks.AllModes.General.Metadata;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Metadata;

public class CheckTitleMarkersTests
{
    [Theory]
    [InlineData("Song (TV Size)", null)]
    [InlineData("Song (Game Ver.)", null)]
    [InlineData("Song (Short Ver.)", null)]
    [InlineData("Song (Movie Ver.)", null)]
    [InlineData("Song (Cut Ver.)", null)]
    [InlineData("Song (Extended Edit)", null)]
    [InlineData("Song (Sped Up Ver.)", null)]
    [InlineData("Song (Nightcore Mix)", null)]
    [InlineData("Song (Nightcore & Cut Ver.)", null)]
    [InlineData("Song -TV version-", "(TV Size)")]
    [InlineData("Song (Game Size)", "(Game Ver.)")]
    [InlineData("Song (Movie Version)", "(Movie Ver.)")]
    [InlineData("Song -extended edit-", "(Extended Edit)")]
    [InlineData("Song (Sped Up & Cut Ver.)", null)]
    public void FlagsNonStandardMarkers(string title, string? expectedCanonical)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        if (expectedCanonical == null)
        {
            Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
            return;
        }

        var problem = Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains(expectedCanonical, problem.message);
    }

    [Fact]
    public void AllowsAlternativeMarkerCasingWhenRomanizedTitleIsAllLowercase()
    {
        using var context = CreateContext("song name (tv size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void AllowsAlternativeMarkerCasingWhenRomanizedTitleIsAllUppercase()
    {
        using var context = CreateContext("SONG NAME (TV SIZE)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void FlagsAlternativeMarkerCasingWhenUppercaseTitleHasLowercaseMarker()
    {
        using var context = CreateContext("SONG NAME (tv size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var problem = Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("(TV Size)", problem.message);
    }

    [Fact]
    public void AllowsCanonicalMarkerCasingRegardlessOfTitleLetterCasing()
    {
        using var context = CreateContext("song name (TV Size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void FlagsAlternativeMarkerCasingWhenRomanizedTitleIsMixedCase()
    {
        using var context = CreateContext("Song Name (tv size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var problem = Assert.Single(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Contains("(TV Size)", problem.message);
    }

    [Fact]
    public void AllowsLivePerformanceMarkers()
    {
        using var context = CreateContext("Song (2020 Tour Live Ver.)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsTvMarkerNotAtEnd()
    {
        using var context = CreateContext("Song (TV Size) (Full Ver.)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Warning && issue.message.Contains("end of the title")
        );
    }

    [Fact]
    public void FlagsExtendedVersionGuideline()
    {
        using var context = CreateContext("Song (Extended Version)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var warning = Assert.Single(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("(Extended Ver.)")
        );
    }

    [Fact]
    public void FlagsLongGuideline()
    {
        using var context = CreateContext("Song (Long)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var warning = Assert.Single(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("(Long Ver.)")
        );
    }

    [Theory]
    [InlineData("Song (Arrange ver.)", "(Arrange Ver.)")]
    [InlineData("Song (vocaloid ver.)", "(Vocaloid Ver.)")]
    [InlineData("Song Title vocaloid ver.", "(Vocaloid Ver.)")]
    public void FlagsNonCanonicalDescriptiveVerMarkers(string title, string expectedSuggestion)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var warning = Assert.Single(
            issues,
            issue =>
                issue.level == Issue.Level.Warning && issue.message.Contains(expectedSuggestion)
        );
    }

    [Theory]
    [InlineData("song name (arrange ver.)")]
    [InlineData("SONG NAME (ARRANGE VER.)")]
    [InlineData("clarity rmx (hayakou boutleg) (cut ver.)")]
    public void AllowsGuidelineVerCasingWhenTitleIsUniformlyStylised(string title)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("Song (Arrange Ver.)")]
    [InlineData("Song (Game Ver.)")]
    public void DoesNotFlagCanonicalDescriptiveVerMarkers(string title)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(
            issues,
            issue => issue.message.Contains("should use guideline format")
        );
    }

    [Theory]
    [InlineData("TERABYTE (\"XETTABYTE\" Long Ver.)")]
    [InlineData("TERABƔTE (\"X∑TTABƔTE\" Long Ver.)")]
    [InlineData("#1f1e33 (Another long \"#ant1p01e\" Ver.)")]
    public void DoesNotFlagQuotedStylisedVerMarkersWithCanonicalSuffix(string title)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(
            issues,
            issue => issue.message.Contains("should use guideline format")
        );
    }

    [Fact]
    public void FlagsQuotedStylisedVerMarkersWithNonCanonicalSuffix()
    {
        using var context = CreateContext("Song (feat. \"artist\" arrange ver.)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Warning
                && issue.message.Contains("should use guideline format")
                && issue.message.Contains("Arrange Ver.)")
        );
    }

    [Theory]
    [InlineData("Pippiquest (Pippi x Mocha Romantic Movie Remix Edition)")]
    [InlineData("You Make My Life 1UP (x127 Long Ver.)")]
    public void SkipsStylisedLongParenthetical(string title)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Empty(issues);
    }

    [Fact]
    public void FlagsOpVersionWithGameTags()
    {
        using var context = CreateContext("Song OP Version", tags: "anime game ost");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Contains(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("(Game Ver.)")
        );
    }

    [Fact]
    public void SuggestsNightcoreWhenTagPresent()
    {
        using var context = CreateContext("Song (Sped Up Ver.)", tags: "nightcore sped up");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        var warning = Assert.Single(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("(Nightcore Mix)")
        );
    }

    [Fact]
    public void DoesNotFlagNightcoreMarkerWithoutNightcoreTag()
    {
        using var context = CreateContext("Song (Nightcore Mix)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.message.Contains("No nightcore tag found"));
    }

    [Fact]
    public void DoesNotFlagCombinedMarkerAsSeparateSpedUp()
    {
        using var context = CreateContext("Song (Sped Up & Cut Ver.)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(
            issues,
            issue => issue.level == Issue.Level.Problem && issue.message.Contains("(Sped Up Ver.)")
        );
    }

    [Theory]
    [InlineData("erase u (sped up & cut ver.)")]
    [InlineData("SLYOZY (ANTVN BOOTLEG) (SPED UP & CUT VER.)")]
    public void DoesNotFlagStylisedCombinedTempoMarkers(string title)
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Fact]
    public void FlagsMissingMarkerInUnicodeTitle()
    {
        using var context = CreateContextWithUnicodeTitle("Song (TV Size)", "歌曲");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Single(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Romanized title field includes \"(TV Size)\"")
                && issue.message.Contains("Unicode title field does not")
        );
    }

    [Fact]
    public void FlagsMissingMarkerInRomanizedTitle()
    {
        using var context = CreateContextWithUnicodeTitle("Song", "歌曲 (TV Size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Single(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Unicode title field includes \"(TV Size)\"")
                && issue.message.Contains("Romanized title field does not")
        );
    }

    [Fact]
    public void DoesNotFlagMatchingMarkersAcrossTitleFields()
    {
        using var context = CreateContextWithUnicodeTitle("Song (TV Size)", "歌曲 (TV Size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.message.Contains("title field does not"));
    }

    [Fact]
    public void DoesNotFlagTitleMarkerMismatchWhenUnicodeMatchesRomanized()
    {
        using var context = CreateContext("Song (TV Size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.message.Contains("title field does not"));
    }

    [Fact]
    public void DoesNotFlagStylisedCombinedTempoMarkersOnUnicodeTitle()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                string.Join(
                    "\n",
                    "osu file format v14",
                    "[General]",
                    "AudioFilename:audio.mp3",
                    "Mode: 0",
                    "[Metadata]",
                    "Title:SLYOZY (ANTVN BOOTLEG) (SPED UP & CUT VER.)",
                    "TitleUnicode:СЛЁЗЫ (ANTVN BOOTLEG) (SPED UP & CUT VER.)",
                    "Artist:Tests",
                    "Creator:Tests",
                    "Version:Test",
                    "[Difficulty]",
                    "CircleSize:4",
                    "HPDrainRate:5",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[Events]",
                    "[TimingPoints]",
                    "[HitObjects]"
                )
            ),
        ]);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    private static CheckTestContext CreateContextWithUnicodeTitle(
        string title,
        string titleUnicode,
        string tags = ""
    ) =>
        CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                string.Join(
                    "\n",
                    "osu file format v14",
                    "[General]",
                    "AudioFilename:audio.mp3",
                    "Mode: 0",
                    "[Metadata]",
                    $"Title:{title}",
                    $"TitleUnicode:{titleUnicode}",
                    "Artist:Tests",
                    "Creator:Tests",
                    "Version:Test",
                    $"Tags:{tags}",
                    "[Difficulty]",
                    "CircleSize:4",
                    "HPDrainRate:5",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[Events]",
                    "[TimingPoints]",
                    "[HitObjects]"
                )
            ),
        ]);

    private static CheckTestContext CreateContext(string title, string tags = "") =>
        CheckTestContext.CreateFromOsuFiles([
            (
                "test.osu",
                string.Join(
                    "\n",
                    "osu file format v14",
                    "[General]",
                    "AudioFilename:audio.mp3",
                    "Mode: 0",
                    "[Metadata]",
                    $"Title:{title}",
                    "Artist:Tests",
                    "Creator:Tests",
                    "Version:Test",
                    $"Tags:{tags}",
                    "[Difficulty]",
                    "CircleSize:4",
                    "HPDrainRate:5",
                    "OverallDifficulty:5",
                    "ApproachRate:5",
                    "SliderMultiplier:1.4",
                    "SliderTickRate:1",
                    "[Events]",
                    "[TimingPoints]",
                    "[HitObjects]"
                )
            ),
        ]);
}
