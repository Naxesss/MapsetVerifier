using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;
using MapsetVerifier.RankingCriteria.Sync;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class RcCatalogueMergerTests
{
    private static readonly RcPage Page = RcPages.Get("catch")!;

    private static RcMergeResult Merge(
        RcCataloguePage? existing,
        string markdown,
        string commit = "commit"
    ) => RcCatalogueMerger.Merge(Page, existing, RcMarkdownParser.Parse(markdown), commit);

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

    [Fact]
    public void WordingChangesAtANewCommitKeepTheReviewedWording()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.** Old text."), "a");
        var reviewedHash = first.Page.Statements[0].Upstream.BodyHash;

        var second = Merge(first.Page, Rules("**Edge dashes must not be used.** New text."), "b");
        var third = Merge(second.Page, Rules("**Edge dashes must not be used.** Newer text."), "c");

        // The first change records the wording before it; later changes keep that review.
        var review = third.Page.Statements[0].LastReview;
        Assert.NotNull(review);
        Assert.Equal("a", review.Commit);
        Assert.Equal(reviewedHash, review.BodyHash);

        // Reported once, when it becomes outdated.
        Assert.Contains(second.Changes, change => change.Type == RcChangeType.Outdated);
        Assert.DoesNotContain(third.Changes, change => change.Type == RcChangeType.Outdated);
    }

    [Fact]
    public void OutdatedStatementsMarkTheirConstantObsolete()
    {
        var first = Merge(
            null,
            Rules("**Edge dashes must not be used.** Old text.", "**Other rule.**"),
            "a"
        );
        var second = Merge(
            first.Page,
            Rules("**Edge dashes must not be used.** New text.", "**Other rule.**"),
            "b"
        );

        var constants = RcConstantsWriter.Write(new RcSource { Commit = "b" }, [second.Page]);

        Assert.Contains(
            "[System.Obsolete(\"Outdated: changed on the osu! wiki since its checks were reviewed at a. "
                + "Update the checks, then run `update-ranking-criteria review catch/edge-dashes-not-used`.\")]\n"
                + "        public const string EdgeDashesNotUsed",
            constants.ReplaceLineEndings("\n")
        );
        Assert.Single(constants.Split('\n'), line => line.Contains("Obsolete"));
    }

    [Fact]
    public void KindChangesAtANewCommitNeedAReview()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.**"), "a");
        var second = Merge(
            first.Page,
            Rules("**Edge dashes must not be used.**").Replace("#### Rules", "#### Guidelines"),
            "b"
        );

        Assert.Equal(RcKind.Rule, second.Page.Statements[0].LastReview?.Kind);
    }

    [Fact]
    public void ReparsingTheSameCommitDoesNotNeedAReview()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.** Old text."), "a");
        var second = Merge(first.Page, Rules("**Edge dashes must not be used.** New text."), "a");

        Assert.Null(second.Page.Statements[0].LastReview);
    }

    [Fact]
    public void ChangingBackToTheReviewedWordingClearsTheReview()
    {
        var first = Merge(null, Rules("**Edge dashes must not be used.** Old text."), "a");
        var second = Merge(first.Page, Rules("**Edge dashes must not be used.** New text."), "b");
        var third = Merge(second.Page, Rules("**Edge dashes must not be used.** Old text."), "c");

        Assert.NotNull(second.Page.Statements[0].LastReview);
        Assert.Null(third.Page.Statements[0].LastReview);
    }
}
