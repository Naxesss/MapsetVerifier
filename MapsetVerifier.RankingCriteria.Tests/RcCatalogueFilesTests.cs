using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Sync;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

public class RcCatalogueFilesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "rc-" + Guid.NewGuid());
    private readonly string dataDir;

    /// <summary> Copies the real catalogue, so every page the files expect is there. </summary>
    public RcCatalogueFilesTests()
    {
        dataDir = Path.Combine(root, "Data");
        var source = RcDataDirectory.Find();

        Directory.CreateDirectory(Path.Combine(dataDir, "catalogue"));
        File.Copy(Path.Combine(source, "source.json"), Path.Combine(dataDir, "source.json"));
        foreach (var file in Directory.GetFiles(Path.Combine(source, "catalogue")))
            File.Copy(file, Path.Combine(dataDir, "catalogue", Path.GetFileName(file)));
    }

    public void Dispose() => Directory.Delete(root, true);

    private RcStatement MarkOutdated(string pageKey)
    {
        var page = RcCatalogueFiles.ReadPage(dataDir, pageKey);
        var statement = page.Statements.First(statement => !statement.IsRetired);
        statement.LastReview = new RcReview { Commit = "0123456789abcdef", Kind = RcKind.Rule };
        RcCatalogueFiles.WritePage(dataDir, page);

        return statement;
    }

    [Fact]
    public void MarkReviewedClearsTheReviewAndTheObsoleteWarning()
    {
        var outdated = MarkOutdated("catch");
        var other = MarkOutdated("osu");

        var reviewed = RcCatalogueFiles.MarkReviewed(
            dataDir,
            [outdated.Id, "catch/not-a-statement"]
        );

        Assert.Equal([outdated.Id], reviewed);
        // The real catalogue may have statements of its own waiting for a review.
        var pending = RcCatalogueFiles
            .ReadPages(dataDir)
            .SelectMany(RcCatalogueFiles.PendingReview)
            .Select(statement => statement.Id)
            .ToList();
        Assert.DoesNotContain(outdated.Id, pending);
        Assert.Contains(other.Id, pending);

        var constants = File.ReadAllText(Path.Combine(root, "RC.g.cs"));
        Assert.DoesNotContain($"review {outdated.Id}`", constants);
        Assert.Contains($"review {other.Id}`", constants);
    }

    [Fact]
    public void MarkReviewedLeavesFilesAloneWhenNothingWasPending()
    {
        var reviewed = RcCatalogueFiles.MarkReviewed(dataDir, ["catch/not-a-statement"]);

        Assert.Empty(reviewed);
        Assert.False(File.Exists(Path.Combine(root, "RC.g.cs")));
    }
}
