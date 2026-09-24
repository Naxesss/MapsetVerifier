using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;
using MapsetVerifier.RankingCriteria.Sync;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class RcCatalogueMergerTests
{
    private static readonly RcPage Page = RcPages.Get("catch")!;

    private static RcMergeResult Merge(RcCataloguePage? existing, string markdown) =>
        RcCatalogueMerger.Merge(Page, existing, RcMarkdownParser.Parse(markdown), "commit");

    private static string Rules(params string[] statements) =>
        "# Title\n\n## Overall\n\n### General\n\n#### Rules\n\n"
        + string.Join("\n", statements.Select(statement => "- " + statement))
        + "\n";

    [Fact]
    public void NewStatementsGetDraftIds()
    {
        var result = Merge(null, Rules("**Edge dashes must not be used.**"));

        Assert.Equal("catch/edge-dashes-not-used", result.Page.Statements[0].Id);
        Assert.Equal(RcChangeType.Added, result.Changes.Single().Type);
    }

    [Fact]
    public void IdsAndCurationSurviveInsertionsAndEdits()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.** Old text."));
        first.Page.Statements[0].Curation.Automation = RcAutomation.Automatable;
        // Renamed by hand, the merge must not derive it again.
        first.Page.Statements[0].Id = "catch/general/edge-dashes";

        var second = Merge(
            first.Page,
            Rules("**A new rule above it.**", "**Edge dashes must not be used.** New text.")
        );

        var edgeDash = second.Page.Statements.Single(statement =>
            statement.Id == "catch/general/edge-dashes"
        );
        Assert.Equal(RcAutomation.Automatable, edgeDash.Curation.Automation);
        Assert.Contains(
            second.Changes,
            change => change.Type == RcChangeType.Edited && change.Id == edgeDash.Id
        );
        Assert.Contains(second.Changes, change => change.Type == RcChangeType.Added);
    }

    [Fact]
    public void RewordedLeadsKeepTheirId()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used in Salads.**"));
        var second = Merge(first.Page, Rules("**Edge dashes must never be used in Salads.**"));

        Assert.Equal(first.Page.Statements[0].Id, second.Page.Statements[0].Id);
        Assert.Equal(RcChangeType.Reworded, second.Changes.Single().Type);
    }

    [Fact]
    public void RemovedStatementsAreRetiredAndCanBeRevived()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.**", "**Other rule.**"));
        var second = Merge(first.Page, Rules("**Other rule.**"));

        var retired = second.Page.Statements.Single(statement => statement.IsRetired);
        Assert.Equal("commit", retired.RetiredAt);
        Assert.Contains(second.Changes, change => change.Type == RcChangeType.Retired);

        var third = Merge(
            second.Page,
            Rules("**Edge dashes must not be used.**", "**Other rule.**")
        );

        Assert.DoesNotContain(third.Page.Statements, statement => statement.IsRetired);
        Assert.Contains(third.Changes, change => change.Type == RcChangeType.Revived);
    }
}
