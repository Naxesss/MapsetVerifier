using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class RcMarkdownParserTests
{
    private const string Page = """
        # osu!catch ranking criteria

        - **Not a statement, there is no Rules heading above this.**

        ## Overall

        ### General

        #### Rules

        - **Your map must be possible to SS.** This means it must be possible to catch all [fruits](/wiki/Fruit).
        - **[Edge dashes](/wiki/Gameplay/Edge_dash) must not be used.**
          Continued on the next line. <!-- maintainer note -->

        #### Guidelines

        - **Combos should not reach unreasonable lengths.**

        ### Spread

        #### Rules

        - **If the drain time of each difficulty is...**
          - **...lower than 2:30**, the lowest difficulty cannot be harder than a Salad.
          - Plain example which belongs to its parent.

        **Break times may be combined with drain time to meet the above thresholds.**

        ## Difficulty-specific

        ### ![](/wiki/shared/diff/normal-c.png?20211215) Salad

        #### Rules

        - **Hyperdashes of any kind are disallowed.**

        ##### Rules: Nested kind heading

        - **Nested kind headings are not part of the path.**

        #### Difficulty setting guidelines

        - [Approach rate](/wiki/Beatmap/Approach_rate) should be 6 or lower. Extra text.
        - `(TV Size)`
          - Add this marker at the end, etc. when the song was on TV.

        ## Notes

        - **Not a statement, the Rules heading above is out of scope.**
        """;

    private static List<ParsedStatement> Parse() => RcMarkdownParser.Parse(Page);

    [Fact]
    public void OnlyStatementsBelowKindHeadingsAreParsed()
    {
        var leads = Parse().Select(statement => statement.Upstream.Lead).ToList();

        Assert.Equal(
            [
                "Your map must be possible to SS.",
                "Edge dashes must not be used.",
                "Combos should not reach unreasonable lengths.",
                "If the drain time of each difficulty is...",
                "...lower than 2:30, the lowest difficulty cannot be harder than a Salad.",
                "Break times may be combined with drain time to meet the above thresholds.",
                "Hyperdashes of any kind are disallowed.",
                "Nested kind headings are not part of the path.",
                "Approach rate should be 6 or lower.",
                "`(TV Size)`: Add this marker at the end, etc. when the song was on TV.",
            ],
            leads
        );
    }

    [Fact]
    public void KindComesFromTheNearestKindHeading()
    {
        var kinds = Parse().Select(statement => statement.Upstream.Kind).ToList();

        Assert.Equal(RcKind.Rule, kinds[0]);
        Assert.Equal(RcKind.Guideline, kinds[2]);
        Assert.Equal(RcKind.Rule, kinds[7]);
    }

    [Fact]
    public void PathExcludesTitleAndKindHeadings()
    {
        var statements = Parse();

        Assert.Equal(["Overall", "General"], statements[0].Upstream.Path);
        Assert.Equal(["Difficulty-specific", "Salad"], statements[6].Upstream.Path);
        Assert.Equal(["Difficulty-specific", "Salad"], statements[7].Upstream.Path);
        Assert.Equal("salad", statements[6].Upstream.Anchor);
    }

    [Fact]
    public void DifficultyComesFromTheHeadingIcon()
    {
        var statements = Parse();

        Assert.Empty(statements[0].Upstream.Difficulties);
        Assert.Equal([Beatmap.Difficulty.Normal], statements[6].Upstream.Difficulties);
    }

    [Fact]
    public void NestedBoldItemsBecomeChildren()
    {
        var statements = Parse();

        Assert.Null(statements[3].ParentIndex);
        Assert.Equal(3, statements[4].ParentIndex);
    }

    [Fact]
    public void LineRangesCoverContinuationLines()
    {
        var statements = Parse();

        Assert.Equal(12, statements[1].Upstream.StartLine);
        Assert.Equal(13, statements[1].Upstream.EndLine);
        // The plain example is a sibling of the nested statement, so it belongs to their parent.
        Assert.Equal(23, statements[3].Upstream.StartLine);
        Assert.Equal(25, statements[3].Upstream.EndLine);
        Assert.Equal(24, statements[4].Upstream.StartLine);
        Assert.Equal(24, statements[4].Upstream.EndLine);
    }

    [Fact]
    public void FingerprintIgnoresFormatting()
    {
        Assert.Equal(
            RcText.Fingerprint("**[Edge dashes](/wiki/Edge_dash) must not be used.**"),
            RcText.Fingerprint("Edge dashes must *not* be used")
        );
    }

    [Fact]
    public void PlainTopLevelItemsAreStatements()
    {
        var statements = Parse();

        Assert.Equal(RcKind.Guideline, statements[8].Upstream.Kind);
        Assert.Equal([Beatmap.Difficulty.Normal], statements[8].Upstream.Difficulties);
        // The nested explanation is part of the marker statement, not a statement of its own.
        Assert.Equal(statements[9].Upstream.StartLine + 1, statements[9].Upstream.EndLine);
    }

    [Fact]
    public void IncompleteSentencesWithNestedStatementsAreIntros()
    {
        var statements = Parse();

        // "If the drain time of each difficulty is..." is finished by its nested statements.
        Assert.True(statements[3].Upstream.Intro);
        Assert.False(statements[4].Upstream.Intro);
        Assert.False(statements[0].Upstream.Intro);
    }

    [Fact]
    public void SlugsAreAscii()
    {
        Assert.Equal("german-u-ue-ss", RcText.Slug("German: `ü` to `ue`, `ß` to `ss`"));
    }
}
