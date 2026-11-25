using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public sealed class RankingCriteriaServiceTests : IDisposable
{
    public RankingCriteriaServiceTests() => CheckerRegistry.ClearChecks();

    public void Dispose()
    {
        RankingCriteriaService.Store = RcStore.Embedded;
        CheckerRegistry.ClearChecks();
    }

    private sealed class LinkingCheck(params string[] rules) : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new() { Message = "Linking." };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new() { ["Key"] = new IssueTemplate(Issue.Level.Warning, "Text.").WithRule(rules) };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet) => [];
    }

    private static RcStatement Statement(
        string id,
        string? parent = null,
        RcKind kind = RcKind.Rule,
        bool intro = false,
        string? retired = null,
        RcAutomation automation = RcAutomation.Automatable,
        RcReview? review = null,
        string anchor = ""
    ) =>
        new()
        {
            Id = id,
            Upstream = new RcUpstream
            {
                Lead = "Lead of " + id,
                Kind = kind,
                Parent = parent,
                Intro = intro,
                Anchor = anchor,
            },
            RetiredAt = retired,
            LastReview = review,
            Curation = new RcCuration { Automation = automation, Notes = "Notes" },
        };

    private static void Use(params RcStatement[] statements) =>
        RankingCriteriaService.Store = new RcStore(
            new RcSource { Repository = "repository", Commit = "commit" },
            new Dictionary<string, string> { ["catch"] = "# Markdown" },
            new Dictionary<string, RcCataloguePage>
            {
                ["catch"] = new() { Page = "catch", Statements = [.. statements] },
                ["unknown"] = new() { Page = "unknown", Statements = [Statement("unknown/a")] },
            }
        );

    private static void Link(params string[] rules) =>
        CheckerRegistry.RegisterCheck(new LinkingCheck(rules));

    private static ApiRcCoverage Coverage(string id) =>
        RankingCriteriaService.GetStatements([id]).Single().Coverage;

    [Fact]
    public void Coverage_IsWhetherAChecksLinksToIt()
    {
        Use(
            Statement("a"),
            Statement("partial", automation: RcAutomation.Partial),
            Statement("outdated", review: new RcReview { Commit = "old" }),
            Statement("none"),
            Statement("manual", automation: RcAutomation.Manual),
            Statement("allowance", kind: RcKind.Allowance),
            Statement("intro", intro: true)
        );
        Link("a", "partial", "outdated");

        Assert.Equal(ApiRcCoverage.Covered, Coverage("a"));
        Assert.Equal(ApiRcCoverage.Partial, Coverage("partial"));
        Assert.Equal(ApiRcCoverage.Outdated, Coverage("outdated"));
        Assert.Equal(ApiRcCoverage.Uncovered, Coverage("none"));
        Assert.Equal(ApiRcCoverage.Manual, Coverage("manual"));
        Assert.Equal(ApiRcCoverage.Informational, Coverage("allowance"));
        Assert.Equal(ApiRcCoverage.Informational, Coverage("intro"));
    }

    [Theory]
    [InlineData("covered", "covered", ApiRcCoverage.Covered)]
    [InlineData("covered", "none", ApiRcCoverage.Partial)]
    [InlineData("none", "none", ApiRcCoverage.Uncovered)]
    [InlineData("covered", "partial", ApiRcCoverage.Partial)]
    [InlineData("outdated", "covered", ApiRcCoverage.Covered)] // Still has its check.
    [InlineData("covered", "manual", ApiRcCoverage.Covered)] // Nothing to check there.
    [InlineData("covered", "allowance", ApiRcCoverage.Covered)]
    [InlineData("covered", "retired", ApiRcCoverage.Covered)]
    [InlineData("manual", "manual", ApiRcCoverage.Uncovered)]
    public void Coverage_IsThatOfItsNestedStatements(
        string first,
        string second,
        ApiRcCoverage expected
    )
    {
        RcStatement Child(string id, string kind) =>
            kind switch
            {
                "partial" => Statement(id, "parent", automation: RcAutomation.Partial),
                "outdated" => Statement(id, "parent", review: new RcReview { Commit = "old" }),
                "manual" => Statement(id, "parent", automation: RcAutomation.Manual),
                "allowance" => Statement(id, "parent", RcKind.Allowance),
                "retired" => Statement(id, "parent", retired: "commit"),
                _ => Statement(id, "parent"),
            };

        Use(Statement("parent"), Child("1", first), Child("2", second));

        var kinds = new[] { ("1", first), ("2", second) };
        Link(
            kinds
                .Where(pair => pair.Item2 is "covered" or "partial" or "outdated")
                .Select(pair => pair.Item1)
                .ToArray()
        );

        Assert.Equal(expected, Coverage("parent"));
    }

    [Fact]
    public void Overview_CountsTheStatementsOfEachPage()
    {
        Use(Statement("a"), Statement("b"), Statement("c"), Statement("d", retired: "commit"));
        Link("a");

        var overview = RankingCriteriaService.GetOverview();
        var page = overview.Pages.Single(page => page.Key == "catch");

        Assert.Equal("repository", overview.Source.Repository);
        Assert.Equal(1, page.Coverage[ApiRcCoverage.Covered]);
        Assert.Equal(2, page.Coverage[ApiRcCoverage.Uncovered]);
        Assert.Equal(3, page.Coverage.Values.Sum());
        Assert.All(
            overview.Pages.Where(other => other.Key != "catch"),
            other => Assert.Equal(0, other.Coverage.Values.Sum())
        );
    }

    [Fact]
    public void Page_HasItsTextAndStatements()
    {
        Use(
            Statement("a", anchor: "section"),
            Statement("b", "a"),
            Statement("gone", retired: "commit")
        );
        Link("a");

        var page = RankingCriteriaService.GetPage("catch")!;

        Assert.Equal("# Markdown", page.Markdown);
        Assert.Equal(["a", "b"], page.Statements.Select(statement => statement.Id));

        var (first, second) = (page.Statements[0], page.Statements[1]);
        Assert.EndsWith("#section", first.WikiUrl);
        Assert.DoesNotContain("#", second.WikiUrl);
        Assert.Null(first.ParentLead);
        Assert.Equal("Lead of a", second.ParentLead);
        Assert.Equal("Linking.", Assert.Single(first.Links).CheckName);
        Assert.Empty(second.Links);

        // Neither has a text.
        Assert.Null(RankingCriteriaService.GetPage("osu"));
        Assert.Null(RankingCriteriaService.GetPage("unknown"));
    }

    [Fact]
    public void Statements_IncludeRetiredOnes_ButNotUnknown()
    {
        Use(
            Statement("a"),
            Statement("gone", retired: "commit", review: new RcReview { Commit = "x" })
        );

        var statements = RankingCriteriaService.GetStatements([
            "gone",
            "a",
            "a",
            "other",
            "unknown/a",
        ]);

        Assert.Equal(["gone", "a"], statements.Select(statement => statement.Id));
        Assert.True(statements[0].Retired);
        Assert.Equal("x", statements[0].LastReview!.Commit);
        Assert.Null(statements[1].LastReview);
    }

    [Fact]
    public void Rules_AreThoseOfTheModeOfTheIssue()
    {
        Use(Statement("a"));
        var template = new IssueTemplate(Issue.Level.Warning, "Text.").WithRule("a", "unknown");

        Issue In(Beatmap.Mode mode) =>
            new(
                template,
                new Beatmap(SongFolder.Osu(mode: (int)mode), "song", $"{Guid.NewGuid()}.osu")
            );

        Assert.Equal(["a", "unknown"], RankingCriteriaService.RuleIdsFor(In(Beatmap.Mode.Catch)));
        Assert.Equal(["unknown"], RankingCriteriaService.RuleIdsFor(In(Beatmap.Mode.Taiko)));
        Assert.Equal(
            ["a", "unknown"],
            RankingCriteriaService.RuleIdsFor(new Issue(template, null))
        );
    }
}
