using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;
using MapsetVerifier.RankingCriteria.Sync;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class RcEdgeTests
{
    private static List<ParsedStatement> Rules(string body, string heading = "Rules") =>
        RcMarkdownParser.Parse($"# Title\n\n## Overall\n\n### {heading}\n\n{body}\n");

    private static string[] Leads(string body) =>
        Rules(body).Select(statement => statement.Upstream.Lead).ToArray();

    [Fact]
    public void CodeBlocksAndComments_AreLeftOut()
    {
        Assert.Equal(
            ["First.", "Second.", "Third."],
            Leads(
                """
                - **First.**

                ```
                - **In a code block.**
                ```

                <!-- A note
                - **In a comment.**
                which ends here -->

                - **Second.**

                <!-- On one line. -->

                - **Third.**
                """
            )
        );
    }

    [Theory]
    [InlineData("- **Whole sentence.** More.", "Whole sentence.")]
    [InlineData("- **Really!** More.", "Really!")]
    [InlineData("- **Start of it** and the rest. More.", "Start of it and the rest.")]
    [InlineData("- **Start**, and the rest", "Start, and the rest")]
    [InlineData("- **Start**. More.", "Start.")]
    [InlineData("- **Minimum width:** 160px", "Minimum width: 160px")]
    [InlineData("- **As follows:**", "As follows:")]
    [InlineData("- Plain sentence. More.", "Plain sentence.")]
    [InlineData("- Only one sentence.", "Only one sentence.")]
    [InlineData("- Without an end", "Without an end")]
    [InlineData("**A paragraph.**", "A paragraph.")]
    public void Lead_IsTheFirstSentence(string body, string expected)
    {
        Assert.Equal([expected], Leads(body));
    }

    [Theory]
    [InlineData(
        "- **Japanese**\n  - Use the Hepburn system. More.",
        "Japanese: Use the Hepburn system."
    )]
    [InlineData("- **Japanese:**\n  Use the Hepburn system.", "Japanese: Use the Hepburn system.")]
    [InlineData("- **Japanese**\n\n  Use Hepburn.", "Japanese: Use Hepburn.")]
    [InlineData("- **Japanese**\n  ![](a.png)", "Japanese")]
    [InlineData("- **Four words are enough**\n  Use Hepburn.", "Four words are enough")]
    public void ShortLead_IsCompletedByTheNextLine(string body, string expected)
    {
        Assert.Equal([expected], Leads(body));
    }

    [Fact]
    public void Prose_IsNoStatement()
    {
        Assert.Empty(Leads("Some plain prose.\n\n  indented prose"));
        Assert.Empty(Rules("- **Something.**", "Notes"));
    }

    [Theory]
    [InlineData("Rules", RcKind.Rule)]
    [InlineData("Rules: Markers", RcKind.Rule)]
    [InlineData("Guidelines", RcKind.Guideline)]
    [InlineData("Difficulty setting guidelines", RcKind.Guideline)]
    [InlineData("Allowances", RcKind.Allowance)]
    public void Kinds(string heading, RcKind expected)
    {
        Assert.Equal(expected, Assert.Single(Rules("- **Something.**", heading)).Upstream.Kind);
    }

    [Theory]
    [InlineData("easy", new[] { Beatmap.Difficulty.Easy })]
    [InlineData("normal", new[] { Beatmap.Difficulty.Normal })]
    [InlineData("hard", new[] { Beatmap.Difficulty.Hard })]
    [InlineData("insane", new[] { Beatmap.Difficulty.Insane })]
    [InlineData("expert", new[] { Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra })]
    [InlineData("extra", new[] { Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra })]
    [InlineData("unknown", new Beatmap.Difficulty[0])]
    public void Difficulties(string icon, Beatmap.Difficulty[] expected)
    {
        Assert.Equal(
            expected,
            Assert
                .Single(
                    RcMarkdownParser.Parse(
                        $"# T\n\n### ![](/wiki/shared/diff/{icon}-o.png) Name\n\n#### Rules\n\n- **A.**"
                    )
                )
                .Upstream.Difficulties
        );
    }

    [Theory]
    [InlineData("", "", 1)]
    [InlineData("a b", "", 0)]
    [InlineData("a b", "a b", 1)]
    [InlineData("a b", "a c", 1d / 3)]
    public void Similarity(string a, string b, double expected)
    {
        Assert.Equal(expected, RcText.Similarity(a, b), 6);
    }

    [Theory]
    [InlineData("must be in the", "statement")]
    [InlineData("Hit objects must never be off-screen in 4:3", "hit-objects-never-off-screen")]
    [InlineData("Café", "cafe")]
    public void Slugs(string text, string expected)
    {
        Assert.Equal(expected, RcText.Slug(text));
    }

    [Theory]
    [InlineData("edge-dashes", "EdgeDashes")]
    [InlineData("-a--b-", "AB")]
    [InlineData("", "")]
    public void PascalCase(string slug, string expected)
    {
        Assert.Equal(expected, RcText.PascalCase(slug));
    }

    private static readonly RcPage Page = RcPages.Get("catch")!;

    private static RcMergeResult Merge(
        RcCataloguePage? existing,
        string markdown,
        string commit = "a"
    ) => RcCatalogueMerger.Merge(Page, existing, RcMarkdownParser.Parse(markdown), commit);

    private static string Sections(params (string Section, string Rule)[] rules) =>
        "# T\n\n"
        + string.Concat(rules.Select(r => $"### {r.Section}\n\n#### Rules\n\n- **{r.Rule}**\n\n"));

    [Fact]
    public void Ids_AreBySection_AndUnique()
    {
        Assert.Equal(
            [
                "catch/hyperdashes-disallowed",
                "catch/salad/hyperdashes-disallowed",
                "catch/hyperdashes-disallowed-2",
                "catch/hyperdashes-disallowed-3",
            ],
            Merge(
                null,
                Sections(
                    ("General", "Hyperdashes are disallowed."),
                    ("Salad", "Hyperdashes are disallowed."),
                    ("Catch", "Hyperdashes are disallowed."),
                    ("General", "Hyperdashes are disallowed.")
                )
            )
                .Page.Statements.Select(statement => statement.Id)
        );
        // Without a section.
        Assert.Equal(
            "catch/something",
            Merge(null, "# T\n\n## Rules\n\n- **Something.**").Page.Statements[0].Id
        );
    }

    [Fact]
    public void SameLeadInTwoSections_KeepsItsOwn()
    {
        var first = Merge(null, Sections(("Salad", "No dashes."), ("Platter", "No dashes.")));
        var swapped = Merge(
            first.Page,
            Sections(("Platter", "No dashes."), ("Salad", "No dashes."))
        );

        Assert.Equal(
            first.Page.Statements.Select(statement => statement.Id).Reverse(),
            swapped.Page.Statements.Select(statement => statement.Id)
        );
        Assert.Empty(swapped.Changes);
    }

    [Fact]
    public void MovedStatements_SayWhereTo()
    {
        var first = Merge(null, Sections(("Salad", "Dashes are not allowed here.")));
        var moved = Merge(first.Page, Sections(("Platter", "Dashes are not allowed here.")));

        var change = Assert.Single(moved.Changes);
        Assert.Equal(RcChangeType.Moved, change.Type);
        Assert.Equal("Salad → Platter", change.Detail);
    }

    [Fact]
    public void RetiredStatements_AreKept_AndNotRetiredTwice()
    {
        var first = Merge(null, Sections(("Salad", "Dashes are not allowed here.")));
        var second = Merge(first.Page, "# T", "b");
        var third = Merge(second.Page, "# T", "c");

        Assert.Equal(RcChangeType.Retired, Assert.Single(second.Changes).Type);
        Assert.Empty(third.Changes);
        Assert.Equal("b", Assert.Single(third.Page.Statements).RetiredAt);
    }

    [Fact]
    public void Children_KnowTheirParent()
    {
        var page = Merge(
            null,
            "# T\n\n## Rules\n\n- **If the drain time is...**\n  - **...lower than 2:30**, be careful.\n"
        ).Page;

        Assert.Null(page.Statements[0].Upstream.Parent);
        Assert.Equal(page.Statements[0].Id, page.Statements[1].Upstream.Parent);
    }

    [Fact]
    public void Constants_AreNamedAfterTheirIds()
    {
        RcStatement Statement(string id, string? retired = null, RcReview? review = null) =>
            new()
            {
                Id = id,
                Upstream = new RcUpstream { Lead = "a < b & c", Kind = RcKind.Rule },
                RetiredAt = retired,
                LastReview = review,
            };

        var code = RcConstantsWriter.Write(
            new RcSource { Repository = "ppy/osu-wiki", Commit = "abc" },
            [
                new RcCataloguePage
                {
                    Page = "catch",
                    Statements =
                    [
                        Statement("catch/edge-dashes"),
                        Statement("catch/edge--dashes"),
                        Statement("catch/edge---dashes"),
                        Statement("catch/salad/no-dashes"),
                        Statement("catch/4k-only"),
                        Statement("catch/gone", "b"),
                        Statement("catch/old", review: new RcReview { Commit = "0123456789" }),
                    ],
                },
                new RcCataloguePage { Page = "unknown", Statements = [Statement("unknown/a")] },
                new RcCataloguePage { Page = "mania", Statements = [Statement("mania/gone", "b")] },
                new RcCataloguePage { Page = "taiko", Statements = [Statement("taiko/a")] },
            ]
        );

        foreach (
            var expected in new[]
            {
                "string EdgeDashes = \"catch/edge-dashes\"",
                "string EdgeDashes2 = \"catch/edge--dashes\"",
                "string EdgeDashes3 = \"catch/edge---dashes\"",
                "string Salad_NoDashes = ",
                "string _4kOnly = ",
                "a &lt; b &amp; c",
                "reviewed at 01234567.",
                "class Taiko",
            }
        )
            Assert.Contains(expected, code);

        foreach (var unexpected in new[] { "gone", "unknown", "class Mania" })
            Assert.DoesNotContain(unexpected, code);
    }

    [Fact]
    public void DataDirectory_IsFoundFromTheCheckout()
    {
        Assert.True(Directory.Exists(RcDataDirectory.Find()));
    }

    [Fact]
    public void ContentUsage_WithoutANameOrNotes()
    {
        var artists = ContentUsagePermissions.Parse(
            """
            | Artist | Status | Notes |
            | :-- | :-: | :-- |
            | Allowed | ![][true] | |
            | Partly | ![][partial] |
            | Not | ![][false] | Some notes |
            | | ![][false] | Nameless |
            | ![][false] | Out of place |
            """
        );

        Assert.Equal(["Partly", "Not"], artists.Select(artist => artist.Name));
        Assert.All(artists, artist => Assert.Null(artist.Notes));
        Assert.Equal(
            [ContentUsageStatus.AllowedWithExceptions, ContentUsageStatus.Disallowed],
            artists.Select(artist => artist.Status)
        );
        Assert.Empty(ContentUsagePermissions.FindIn(artists, null, "", "Someone"));
        Assert.Contains("#artist-permissions", ContentUsagePermissions.WikiUrl);
    }

    [Theory]
    [InlineData("- **Three words only**\n  More of it.", "Three words only: More of it.")]
    [InlineData("- **Easily four words here**\n  More of it.", "Easily four words here")]
    public void UpToThreeWords_AreAShortLead(string body, string expected)
    {
        Assert.Equal([expected], Leads(body));
    }

    /// <summary> Only with rules of its own below it, rather than anywhere. </summary>
    [Fact]
    public void Intro_NeedsItsOwnChildren()
    {
        var statements = Rules(
            "- **With rules...**\n  - **...below it.**\n- **Without any rules...**\n- **A whole rule.**"
        );

        Assert.Equal(
            [true, false, false, false],
            statements.Select(statement => statement.Upstream.Intro)
        );
    }

    [Fact]
    public void HeadingsInBold_AreNoStatements()
    {
        Assert.Equal(["After."], Leads("#### **Heading.**\n\n- **After.**"));
    }

    /// <summary> What was retired stays so, and a new rule like it is a new one. </summary>
    [Fact]
    public void RetiredStatements_AreNotReworded()
    {
        var first = Merge(null, Sections(("Salad", "Dashes are not allowed over here at all.")));
        var retired = Merge(first.Page, "# T", "b");
        var after = Merge(
            retired.Page,
            Sections(("Salad", "Dashes are not allowed over there at all.")),
            "c"
        );

        Assert.Equal(RcChangeType.Added, Assert.Single(after.Changes).Type);
        Assert.Equal(2, after.Page.Statements.Count);
    }

    /// <summary> One old rule can only become one of the new ones. </summary>
    [Fact]
    public void Rewording_IsOnlyOfOne()
    {
        var first = Merge(null, Sections(("Salad", "Dashes are not allowed over here at all.")));
        var after = Merge(
            first.Page,
            Sections(
                ("Salad", "Dashes are not allowed over there at all."),
                ("Salad", "Dashes are not allowed over where at all.")
            )
        );

        Assert.Equal(
            [RcChangeType.Added, RcChangeType.Reworded],
            after.Changes.Select(change => change.Type).Order()
        );
    }
}
