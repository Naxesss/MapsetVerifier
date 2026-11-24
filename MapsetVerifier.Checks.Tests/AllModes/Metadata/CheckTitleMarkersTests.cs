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
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title("SLYOZY (ANTVN BOOTLEG) (SPED UP & CUT VER.)")
                .TitleUnicode("СЛЁЗЫ (ANTVN BOOTLEG) (SPED UP & CUT VER.)")
                .Artist("Tests")
                .Creator("Tests")
        );

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
    }

    [Theory]
    [InlineData("Song Name (TV SIZE)", "(TV SIZE)", "(TV Size)")]
    [InlineData("Song Name (CUT VER.)", "(CUT VER.)", "(Cut Ver.)")]
    public void WarnsInsteadOfProblemForFullCapsMarker(
        string title,
        string written,
        string canonical
    )
    {
        using var context = CreateContext(title);

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
        var warning = Assert.Single(
            issues,
            issue => issue.level == Issue.Level.Warning && issue.message.Contains("in capitals")
        );
        Assert.Contains(written, warning.message);
        Assert.Contains(canonical, warning.message);
    }

    [Fact]
    public void WarnsOnceWhenBothTitleFieldsUseTheSameFullCapsMarker()
    {
        using var context = CreateContextWithUnicodeTitle("Uta (TV SIZE)", "歌 (TV SIZE)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.DoesNotContain(issues, issue => issue.level == Issue.Level.Problem);
        Assert.Single(issues, issue => issue.message.Contains("in capitals"));
    }

    [Fact]
    public void KeepsProblemWhenFullCapsMarkerDiffersBetweenTitleFields()
    {
        using var context = CreateContextWithUnicodeTitle("Uta (TV SIZE)", "歌 (TV Size)");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Contains(
            issues,
            issue =>
                issue.level == Issue.Level.Problem
                && issue.message.Contains("Romanized title field")
        );
        Assert.DoesNotContain(issues, issue => issue.message.Contains("in capitals"));
    }

    [Fact]
    public void KeepsProblemForHyphenatedMarkerEvenWhenBothTitleFieldsMatch()
    {
        using var context = CreateContextWithUnicodeTitle("Uta -TV version-", "歌 -TV version-");

        var issues = context.RunGeneralCheck<CheckTitleMarkers>();

        Assert.Contains(issues, issue => issue.level == Issue.Level.Problem);
        Assert.DoesNotContain(issues, issue => issue.message.Contains("in capitals"));
    }

    /// <summary> Long descriptions are part of the title rather than a marker. </summary>
    [Theory]
    [InlineData(40, true)]
    [InlineData(41, false)]
    public void VersionsOfUpTo40Characters_AreStandardized(int length, bool expected)
    {
        var inner = new string('a', length) + " version";

        Assert.Equal(expected, Messages($"Song ({inner})").Count > 0);
    }

    private static CheckTestContext CreateContextWithUnicodeTitle(
        string title,
        string titleUnicode,
        string tags = ""
    ) =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Title(title)
                .TitleUnicode(titleUnicode)
                .Artist("Tests")
                .Creator("Tests")
                .Tags(tags)
        );

    private static CheckTestContext CreateContext(string title, string tags = "") =>
        CheckTestContext.CreateFromOsu(
            new OsuBuilder().Title(title).Artist("Tests").Creator("Tests").Tags(tags)
        );

    private static List<string> Messages(string title, string? unicode = null, string? tags = null)
    {
        var builder = new OsuBuilder()
            .Title(title)
            .TitleUnicode(unicode ?? title)
            .WithDefaultTiming()
            .WithDefaultHitObject();

        if (tags != null)
            builder.Tags(tags);

        using var context = CheckTestContext.CreateFromOsu(builder);

        return context.RunGeneralCheck<CheckTitleMarkers>().Select(issue => issue.message).ToList();
    }

    [Fact]
    public void WithoutATitle_NothingIsSaid()
    {
        Assert.Empty(Messages(""));
        Assert.Empty(Messages("Song", ""));
        Assert.Empty(Messages("", "Song"));
    }

    [Theory]
    [InlineData("Song (TV Size)", "Uta (TV Size)", false)]
    [InlineData("Song (TV Size)", "Uta", true)]
    [InlineData("Song", "Uta (TV Size)", true)]
    [InlineData("Song (Sped Up & Cut Ver.)", "Uta (Sped Up & Cut Ver.)", false)]
    [InlineData("Song (Nightcore & Cut Ver.)", "Uta", true)]
    [InlineData("Song (Live TV Size)", "Uta", false)]
    public void Marker_IsInBothTitles(string title, string unicode, bool mismatch)
    {
        Assert.Equal(
            mismatch,
            Messages(title, unicode).Any(message => message.Contains("is missing from"))
                || Messages(title, unicode)
                    .Any(message => message.Contains("Unicode") && message.Contains("Romanized"))
        );
    }

    [Theory]
    [InlineData("Song (TV Size)", false)]
    [InlineData("Song TV Size", true)]
    [InlineData("Song (TV Size) Remix", false)]
    [InlineData("Song (TV Size) (Remix)", true)]
    [InlineData("Song TV Size)", true)]
    [InlineData("Song )TV Size(", true)]
    [InlineData("Song (TV Size", true)]
    [InlineData("Song (tv size)", true)]
    public void TvSize_IsTheLastParenthetical(string title, bool expected)
    {
        Assert.Equal(
            expected,
            Messages(title).Any(message => message.Contains("end of the title"))
        );
    }

    [Theory]
    [InlineData("Song (OP Version)", "game", true)]
    [InlineData("Song (OP Version)", "videogame", true)]
    [InlineData("Song (OP Version)", "anime", false)]
    [InlineData("Song (Game Ver.)", "game", false)]
    [InlineData("Song", "game", false)]
    public void OpVersions_OfGames_AreGameVersions(string title, string tags, bool expected)
    {
        Assert.Equal(
            expected,
            Messages(title, tags: tags).Any(message => message.Contains("(Game Ver.)"))
        );
    }

    [Theory]
    [InlineData("Song (Extended Version)", "(Extended Ver.)")]
    [InlineData("Song (Long)", "(Long Ver.)")]
    [InlineData("Song (Long Ver.)", null)]
    [InlineData("Song (A Very Long Name Of Over Forty Characters Here Version)", null)]
    [InlineData("Song (Some Remix ver)", null)]
    [InlineData("Song (\"Quoted\" Long version)", "(\"Quoted\" Long Ver.)")]
    [InlineData("Song (piano version)", "(Piano Ver.)")]
    [InlineData("12345", null)]
    public void OtherVersions_AreWrittenInOneWay(string title, string? expected)
    {
        var messages = Messages(title);

        if (expected == null)
            Assert.Empty(messages);
        else
            Assert.Contains(messages, message => message.Contains(expected));
    }

    [Theory]
    [InlineData("Song (Sped Up Ver.)", "Uta (Sped Up Ver.)", 2)]
    [InlineData("Song (Sped Up Ver.)", "Song (Sped Up Ver.)", 1)]
    [InlineData("Song", "Uta (Sped Up Ver.)", 1)]
    [InlineData("Song (Nightcore Mix)", "Uta (Nightcore Mix)", 0)]
    public void SpedUpWithNightcoreTags_IsAskedAbout(string title, string unicode, int expected)
    {
        Assert.Equal(
            expected,
            Messages(title, unicode, "nightcore")
                .Count(message => message.Contains("\"nightcore\""))
        );
    }

    [Theory]
    [InlineData("12 (TV SIZE)", "(TV SIZE)", 1)]
    [InlineData("Song (TV SIZE)", "Uta (TV Size)", 1)]
    [InlineData("Song (TV SIZE)", "", 1)]
    [InlineData("SONG (TV SIZE)", "", 0)]
    [InlineData("song (tv size)", "", 0)]
    [InlineData("song (TV SIZE)", "", 1)]
    [InlineData("SONG (tv size)", "", 1)]
    public void CapitalsOfAMarker_FollowTheRestOfTheTitle(
        string title,
        string unicode,
        int expected
    )
    {
        Assert.Equal(
            expected,
            Messages(title, unicode)
                .Count(message =>
                    message.Contains("incorrect format") || message.Contains("in capitals")
                )
        );
    }

    [Theory]
    [InlineData("Song (Sped Up & Cut Version)", "(Sped Up & Cut Ver.)")]
    [InlineData("Song (sped up & cut ver.)", "(Sped Up & Cut Ver.)")]
    [InlineData("Song (Nightcore & Cut Version)", "(Nightcore & Cut Ver.)")]
    [InlineData("Song -Sped Up Ver.-", "(Sped Up Ver.)")]
    public void CombinedMarkers_AreCorrectedAsOne(string title, string expected)
    {
        Assert.Contains(
            Messages(title),
            message => message.Contains($"incorrect format of \"{expected}\"")
        );
    }
}
