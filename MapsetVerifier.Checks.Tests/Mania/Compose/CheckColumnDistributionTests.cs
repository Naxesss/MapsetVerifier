using MapsetVerifier.Checks.Mania.Compose;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.Compose;

public class CheckColumnDistributionTests
{
    private static readonly int[] Columns =
    [
        ManiaOsu.Column1,
        ManiaOsu.Column2,
        ManiaOsu.Column3,
        ManiaOsu.Column4,
    ];

    /// <summary> Builds a 4 key beatmap holding the given amount of notes in each column. </summary>
    private static CheckTestContext CreateWithColumnCounts(params int[] noteCounts)
    {
        var hitObjects = new List<string>();
        double time = 1000;

        for (var column = 0; column < noteCounts.Length; column++)
        {
            for (var note = 0; note < noteCounts[column]; note++)
            {
                hitObjects.Add(ManiaOsu.Note(time, column: Columns[column]));
                time += 500;
            }
        }

        return CheckTestContext.CreateFromOsu(ManiaOsu.Build(hitObjects: hitObjects));
    }

    [Fact]
    public void OverusedColumn_IsReportedAsWarning()
    {
        // 40 of 100 notes in column 1, i.e. 160% of the 25 note average.
        using var context = CreateWithColumnCounts(40, 20, 20, 20);

        var issues = context.RunBeatmapSetCheck<CheckColumnDistribution>();
        var columnOneIssue = Assert.Single(issues, issue => issue.message.Contains("Column 1"));

        Assert.Equal(Issue.Level.Warning, columnOneIssue.level);
        Assert.Contains("overused", columnOneIssue.message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnderusedColumn_IsReportedAsWarning()
    {
        // 10 of 100 notes in column 1, i.e. 40% of the 25 note average.
        using var context = CreateWithColumnCounts(10, 30, 30, 30);

        var issues = context.RunBeatmapSetCheck<CheckColumnDistribution>();
        var columnOneIssue = Assert.Single(issues, issue => issue.message.Contains("Column 1"));

        Assert.Equal(Issue.Level.Warning, columnOneIssue.level);
        Assert.Contains("underused", columnOneIssue.message, StringComparison.Ordinal);
    }

    /// <summary> Only unused columns are severe enough to be a problem, no matter how skewed the rest is. </summary>
    [Fact]
    public void HeavilySkewedColumns_ReportNoProblems()
    {
        using var context = CreateWithColumnCounts(70, 10, 10, 10);

        var issues = context.RunBeatmapSetCheck<CheckColumnDistribution>();

        Assert.NotEmpty(issues);
        Assert.All(issues, issue => Assert.Equal(Issue.Level.Warning, issue.level));
    }

    [Fact]
    public void UnusedColumn_IsReportedAsProblem()
    {
        using var context = CreateWithColumnCounts(0, 25, 25, 25);

        var issues = context.RunBeatmapSetCheck<CheckColumnDistribution>();
        var columnOneIssue = Assert.Single(issues, issue => issue.message.Contains("Column 1"));

        Assert.Equal(Issue.Level.Problem, columnOneIssue.level);
        Assert.Contains("unused", columnOneIssue.message, StringComparison.Ordinal);
    }

    [Fact]
    public void EvenlyUsedColumns_ReportNoIssues()
    {
        using var context = CreateWithColumnCounts(25, 25, 25, 25);

        var issues = context.RunBeatmapSetCheck<CheckColumnDistribution>();

        Assert.Empty(issues);
    }
}
