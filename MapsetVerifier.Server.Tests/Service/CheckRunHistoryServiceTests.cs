using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public sealed class CheckRunHistoryServiceTests : IDisposable
{
    private readonly SongFolder song = new();
    private readonly SongFolder store = new();
    private int edits;

    public CheckRunHistoryServiceTests()
    {
        CheckRunHistoryService.ConfigurePath(store.Path, "externals");
        Edit();
    }

    public void Dispose()
    {
        song.Dispose();
        store.Dispose();
    }

    /// <summary> Changes the map, as runs of the same files aren't compared. </summary>
    private void Edit() => song.Write("a.osu", SongFolder.Osu(title: new string('a', ++edits)));

    private ApiCheckRunDelta? Run(params (string Message, Issue.Level Level)[] issues) =>
        CheckRunHistoryService.BuildDeltaAndRememberCurrent(
            new BeatmapSet(song.Path),
            new ApiBeatmapSetCheckResult(
                Category("General", []),
                [Category("Hard", issues)],
                new Dictionary<int, ApiCheckDefinition> { { 3, new(3, "A check.", ["Hard"]) } }
            )
        );

    private static ApiCategoryCheckResult Category(
        string name,
        (string Message, Issue.Level Level)[] issues
    ) =>
        new(
            name,
            null,
            issues.Select(issue => new ApiCheckResult(3, issue.Message, issue.Level, [])).ToList(),
            null,
            null,
            null
        );

    private static string[] Messages(IEnumerable<ApiCheckDeltaIssue> issues) =>
        issues.Select(issue => issue.Message).ToArray();

    [Fact]
    public void FirstRun_HasNothingToCompareWith()
    {
        Assert.Null(Run(("a", Issue.Level.Problem)));
        Assert.Single(
            Directory.GetFiles(Path.Combine(store.Path, "externals", "check-runs"), "*.json")
        );
    }

    [Fact]
    public void UnchangedMap_IsNotCompared()
    {
        Run(("a", Issue.Level.Problem));

        Assert.Null(Run(("b", Issue.Level.Problem)));
    }

    [Fact]
    public void ChangedMap_IsComparedWithTheLastRun()
    {
        Run(
            ("fixed", Issue.Level.Problem),
            ("worse", Issue.Level.Minor),
            ("better", Issue.Level.Problem),
            ("same", Issue.Level.Warning)
        );
        Edit();

        var delta = Run(
            ("new", Issue.Level.Problem),
            ("worse", Issue.Level.Warning),
            ("better", Issue.Level.Minor),
            ("same", Issue.Level.Warning)
        )!.Value;

        Assert.Equal(["new"], Messages(delta.NewIssues));
        Assert.Equal(["fixed"], Messages(delta.ResolvedIssues));
        Assert.Equal(["same"], Messages(delta.UnchangedIssues));

        var worse = Assert.Single(delta.WorsenedIssues);
        Assert.Equal("worse", worse.Message);
        Assert.Equal(Issue.Level.Minor, worse.PreviousLevel);
        Assert.Equal(Issue.Level.Warning, worse.Level);
        Assert.Equal("A check.", worse.CheckName);
        Assert.Equal("Hard", worse.Category);

        Assert.Equal(["better"], Messages(delta.ImprovedIssues));
        Assert.True(delta.PreviousRunAt <= delta.CurrentRunAt);
    }

    [Fact]
    public void SameIssueTwice_IsStillFound()
    {
        Run(("a", Issue.Level.Problem), ("a", Issue.Level.Problem));
        Edit();

        Assert.Equal(["a"], Messages(Run(("a", Issue.Level.Problem))!.Value.UnchangedIssues));
    }

    [Fact]
    public void ClearedHistory_StartsOver()
    {
        Run(("a", Issue.Level.Problem));
        Edit();

        CheckRunHistoryService.ClearHistory(new BeatmapSet(song.Path));

        Assert.Null(Run(("a", Issue.Level.Problem)));
    }

    [Fact]
    public void BrokenHistory_IsIgnored()
    {
        Run(("a", Issue.Level.Problem));
        File.WriteAllText(
            Directory.GetFiles(Path.Combine(store.Path, "externals", "check-runs"))[0],
            "not json"
        );
        Edit();

        Assert.Null(Run(("a", Issue.Level.Problem)));
    }

    [Fact]
    public void ClearedHistory_HasNothingToCompareWith()
    {
        // Nothing to clear yet.
        CheckRunHistoryService.ClearHistory(new BeatmapSet(song.Path));

        Run(("a", Issue.Level.Problem));
        CheckRunHistoryService.ClearHistory(new BeatmapSet(song.Path));
        Edit();

        Assert.Null(Run(("b", Issue.Level.Problem)));
    }

    [Fact]
    public void ChecksNoLongerThere_AreNamedByTheirId()
    {
        ApiCheckRunDelta? RunUnknown(string message)
        {
            Edit();

            return CheckRunHistoryService.BuildDeltaAndRememberCurrent(
                new BeatmapSet(song.Path),
                new ApiBeatmapSetCheckResult(
                    Category("General", [(message, Issue.Level.Warning)]),
                    [],
                    new Dictionary<int, ApiCheckDefinition>()
                )
            );
        }

        RunUnknown("a");
        var delta = RunUnknown("b")!;

        Assert.Contains("Check #3", System.Text.Json.JsonSerializer.Serialize(delta));
    }
}
