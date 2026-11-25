using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.Server.Controller;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Snapshots.Store;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

/// <summary> How the issues of a check run are handed to the client, and documentation of the checks. </summary>
public sealed class BeatmapServiceCheckTests : IDisposable
{
    private readonly SongFolder song = new();

    public BeatmapServiceCheckTests() => CheckerRegistry.ClearChecks();

    public void Dispose()
    {
        CheckerRegistry.ClearChecks();
        song.Dispose();
    }

    private ApiBeatmapSetCheckResult Run(bool snapshot = false) =>
        BeatmapService.RunBeatmapSetChecks(
            song.Path,
            includeCheckRunDelta: false,
            createSnapshot: snapshot
        );

    private static string[] Messages(ApiCategoryCheckResult category) =>
        category.CheckResults.Select(result => result.Message).ToArray();

    [Fact]
    public void Issues_AreSortedIntoGeneralAndTheirDifficulty()
    {
        song.Write("a.osu", SongFolder.Osu("Easy", beatmapId: 12));
        song.Write("b.osu", SongFolder.Osu("Hard"));
        CheckerRegistry.RegisterCheck(new StubGeneral());
        CheckerRegistry.RegisterCheck(new StubBeatmap());
        CheckerRegistry.RegisterCheck(new StubSet());

        var result = Run();
        var difficulties = result.Difficulties.ToDictionary(difficulty => difficulty.Category);

        // Issues of a set check without a difficulty concern the set as a whole.
        Assert.Equal(["general", "whole set"], Messages(result.General).Order());
        Assert.Equal(["in Easy", "set in Easy"], Messages(difficulties["Easy"]).Order());
        Assert.Equal(["in Hard", "set in Hard"], Messages(difficulties["Hard"]).Order());
        Assert.Equal(12ul, difficulties["Easy"].BeatmapId);
        Assert.Equal(Beatmap.Difficulty.Easy, difficulties["Easy"].DifficultyLevel);
    }

    [Fact]
    public void Checks_ListWhereTheyFoundSomething()
    {
        song.Write("a.osu", SongFolder.Osu("Easy"));
        song.Write("b.osu", SongFolder.Osu("Hard"));
        CheckerRegistry.RegisterCheck(new StubGeneral { Message = null });
        CheckerRegistry.RegisterCheck(new StubSet());

        var result = Run();

        var (id, definition) = Assert.Single(result.Checks);
        Assert.Equal(1, id);
        Assert.Equal("Stub set.", definition.Name);
        Assert.Equal(["General", "Easy", "Hard"], definition.Difficulties);
        Assert.All(result.General.CheckResults, issue => Assert.Equal(1, issue.Id));
    }

    [Fact]
    public void Issues_ForOtherDifficultyLevels_AreLeftOut()
    {
        song.Write("a.osu", SongFolder.Osu("Easy"));
        song.Write("b.osu", SongFolder.Osu("Hard"));
        CheckerRegistry.RegisterCheck(new StubBeatmap { For = Beatmap.Difficulty.Hard });

        var difficulties = Run().Difficulties.ToDictionary(difficulty => difficulty.Category);

        Assert.Empty(Messages(difficulties["Easy"]));
        Assert.Equal(["in Hard"], Messages(difficulties["Hard"]));
    }

    [Fact]
    public void Rules_AreThoseOfTheBeatmapsMode()
    {
        song.Write("a.osu", SongFolder.Osu("Easy"));
        song.Write("b.osu", SongFolder.Osu("Cup", mode: 2));
        var osuRule = RcStore.Embedded.GetStatements("osu").First().Id;
        var generalRule = RcStore.Embedded.GetStatements("general").First().Id;
        CheckerRegistry.RegisterCheck(new StubBeatmap { Rules = [osuRule, generalRule] });

        var difficulties = Run().Difficulties.ToDictionary(difficulty => difficulty.Category);

        Assert.Equal([osuRule, generalRule], difficulties["Easy"].CheckResults.Single().RuleIds);
        Assert.Equal([generalRule], difficulties["Cup"].CheckResults.Single().RuleIds);
    }

    [Fact]
    public void Override_RunsADifficultyAsAnotherLevel()
    {
        song.Write("a.osu", SongFolder.Osu("Easy"));
        song.Write("b.osu", SongFolder.Osu("Hard"));
        CheckerRegistry.RegisterCheck(new StubGeneral());
        CheckerRegistry.RegisterCheck(new StubBeatmap { For = Beatmap.Difficulty.Hard });

        var result = BeatmapService
            .RunDifficultyCheckWithOverride(song.Path, "Easy", Beatmap.Difficulty.Hard)!
            .Value;

        Assert.Equal(["in Easy"], Messages(result.CategoryResult));
        Assert.Equal(Beatmap.Difficulty.Hard, result.CategoryResult.DifficultyLevel);
        Assert.Equal(["Easy"], Assert.Single(result.Checks).Value.Difficulties);
    }

    [Fact]
    public void Override_OfAnUnknownDifficulty_IsNull()
    {
        song.Write("a.osu", SongFolder.Osu("Easy"));

        Assert.Null(
            BeatmapService.RunDifficultyCheckWithOverride(
                song.Path,
                "Nope",
                Beatmap.Difficulty.Hard
            )
        );
    }

    [Fact]
    public void Structure_ListsDifficultiesInOrder()
    {
        song.Write("a.osu", SongFolder.Osu("[7K] Hard", mode: 3, circleSize: "7"));
        song.Write("b.osu", SongFolder.Osu("Hard", beatmapId: 5));
        song.Write("c.osu", SongFolder.Osu("Easy"));

        var difficulties = BeatmapService.GetBeatmapSetStructure(song.Path).Difficulties.ToList();

        Assert.Equal(["Easy", "Hard", "[7K] Hard"], difficulties.Select(d => d.Category));
        Assert.Equal(5ul, difficulties[1].BeatmapId);
        Assert.Equal("7K", difficulties[2].Playstyle!.Label);
        Assert.Null(difficulties[0].Playstyle);
    }

    [Fact]
    public void CheckRun_StoresASnapshotWithItsNumbers()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "");
        song.Write("a.osu", SongFolder.Osu("Easy", beatmapSetId: 77));
        CheckerRegistry.RegisterCheck(new StubGeneral());
        CheckerRegistry.RegisterCheck(new StubBeatmap { Level = Issue.Level.Warning });

        Run(snapshot: true);
        var history = SnapshotService.GetHistory(song.Path);

        Assert.Equal("set-77", history.SetKey);
        var entry = Assert.Single(history.Entries);
        Assert.Equal("checkRun", entry.Trigger);
        Assert.Equal(1, entry.Checks!.Problems);
        Assert.Equal(1, entry.Checks.Warnings);
        Assert.Equal(0, entry.Checks.Minor);
    }

    [Fact]
    public void Snapshots_CanBePinned_AndCompared()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "");
        song.Write("a.osu", SongFolder.Osu("Easy", beatmapId: 1, beatmapSetId: 78));
        var first = Assert.Single(SnapshotService.Capture(song.Path, "  Ready  ").Entries);
        Assert.Equal("Ready", first.Pin);

        song.Write(
            "a.osu",
            SongFolder.Osu(
                "Easy",
                beatmapId: 1,
                beatmapSetId: 78,
                hitObjects: ["256,192,1000,5,0,0:0:0:0:", "100,100,2000,1,0,0:0:0:0:"]
            )
        );
        var history = SnapshotService.Capture(song.Path, " ");
        var latest = history.Entries.First(entry => entry.Id != first.Id);
        Assert.Null(latest.Pin);

        var comparison = SnapshotService.Compare("set-78", first.Id, latest.Id);
        Assert.Equal(1, Assert.Single(comparison.Difficulties).Counts.Changed);

        Assert.Throws<ArgumentException>(() => SnapshotService.Compare("set-78", first.Id, "nope"));
        Assert.Throws<ArgumentException>(() => SnapshotService.SetPin("set-78", "nope", "x"));
        Assert.Equal(
            "Final",
            SnapshotService
                .SetPin("set-78", latest.Id, "Final")
                .Entries.Single(entry => entry.Id == latest.Id)
                .Pin
        );
    }

    [Fact]
    public void SnapshotController_AnswersErrorsWithTheirReason()
    {
        using var store = new SongFolder();
        SnapshotStore.ConfigurePath(store.Path, "");

        var result = (ObjectResult)
            new SnapshotController().Compare(
                new SnapshotCompareRequest
                {
                    SetKey = "set-1",
                    BaseId = "a",
                    TargetId = "b",
                }
            );

        Assert.Equal(500, result.StatusCode);
        var error = (ApiError)result.Value!;
        Assert.Equal("An error occurred while comparing snapshots.", error.Message);
        Assert.Equal("Unknown snapshot.", error.Details);
    }

    [Fact]
    public void EmptyFolder_HasNoSnapshots()
    {
        Assert.Empty(SnapshotService.GetHistory(song.Path).Entries);
        Assert.Empty(SnapshotService.Capture(song.Path, null).Entries);
    }

    [Fact]
    public void Documentation_IsSplitByKindAndMode()
    {
        CheckerRegistry.RegisterCheck(new StubGeneral());
        CheckerRegistry.RegisterCheck(new StubBeatmap { Modes = [Beatmap.Mode.Taiko] });
        CheckerRegistry.RegisterCheck(new StubSet());

        var general = Assert.Single(DocumentationService.GetGeneralDocumentation());
        Assert.Equal("Stub general.", general.Description);
        Assert.Equal(0, general.Id);
        Assert.Equal(4, general.Modes.Length);

        Assert.Equal(
            ["Stub beatmap.", "Stub set."],
            DocumentationService
                .GetBeatmapDocumentation(Beatmap.Mode.Taiko)
                .Select(check => check.Description)
        );
        Assert.Equal(
            ["Stub set."],
            DocumentationService
                .GetBeatmapDocumentation(Beatmap.Mode.Mania)
                .Select(check => check.Description)
        );
    }

    [Fact]
    public void DocumentationDetails_AreMarkdown()
    {
        CheckerRegistry.RegisterCheck(new StubGeneral());

        var details = DocumentationService.GetDocumentationDetails(0)!.Value;

        Assert.Equal("# Purpose\n\nFirst line.\nSecond line.", details.Description);
        var outcome = Assert.Single(details.Outcomes);
        Assert.Equal(Issue.Level.Problem, outcome.Level);
        Assert.Equal("`message`", outcome.Description);
        Assert.Equal("Because.", outcome.Cause);
    }

    [Fact]
    public void DocumentationDetails_OfAnUnknownCheck_AreNotFound()
    {
        Assert.Null(DocumentationService.GetDocumentationDetails(5));
        Assert.IsType<NotFoundObjectResult>(new DocumentationController().GetDetails(5).Result);
    }

    [Fact]
    public void RankingCriteria_LinkToTheChecksEnforcingThem()
    {
        var statement = RcStore
            .Embedded.GetStatements("osu")
            .First(candidate => !candidate.IsRetired && candidate.LastReview == null);
        Assert.Empty(Assert.Single(RankingCriteriaService.GetStatements([statement.Id])).Links);

        CheckerRegistry.RegisterCheck(new StubGeneral());
        CheckerRegistry.RegisterCheck(new StubBeatmap { Rules = [statement.Id] });

        var result = Assert.Single(
            RankingCriteriaService.GetStatements([statement.Id, statement.Id, "unknown"])
        );
        var link = Assert.Single(result.Links);
        Assert.Equal(1, link.CheckId);
        Assert.Equal("Stub beatmap.", link.CheckName);

        var page = RankingCriteriaService.GetPage("osu")!;
        Assert.Contains(page.Statements, candidate => candidate.Id == statement.Id);
        Assert.DoesNotContain(page.Statements, candidate => candidate.Retired);
        Assert.Null(RankingCriteriaService.GetPage("unknown"));
    }

    [Fact]
    public void RankingCriteria_OverviewCountsEveryStatementOnce()
    {
        var overview = RankingCriteriaService.GetOverview();

        Assert.All(
            overview.Pages,
            page =>
                Assert.Equal(
                    RcStore.Embedded.GetStatements(page.Key).Count(s => !s.IsRetired),
                    page.Coverage.Values.Sum()
                )
        );
    }

    private static Dictionary<string, IssueTemplate> Templates(
        Issue.Level level,
        params string[] rules
    ) =>
        new()
        {
            {
                "Template",
                new IssueTemplate(level, "{0}", "message").WithCause("Because.").WithRule(rules)
            },
        };

    private sealed class StubGeneral : GeneralCheck
    {
        public string? Message { get; init; } = "general";

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            Templates(Issue.Level.Problem);

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Message = "Stub general.",
                Documentation = new Dictionary<string, string>
                {
                    { "Purpose", "First line.\n    Second line." },
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (Message != null)
                yield return new Issue(GetTemplate("Template"), null, Message);
        }
    }

    private sealed class StubBeatmap : BeatmapCheck
    {
        public Beatmap.Difficulty? For { get; init; }
        public Issue.Level Level { get; init; } = Issue.Level.Problem;
        public string[] Rules { get; init; } = [];
        public Beatmap.Mode[] Modes { get; init; } = Enum.GetValues<Beatmap.Mode>();

        public override Dictionary<string, IssueTemplate> GetTemplates() => Templates(Level, Rules);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata { Message = "Stub beatmap.", Modes = Modes };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var issue = new Issue(
                GetTemplate("Template"),
                beatmap,
                "in " + beatmap.MetadataSettings.version
            );
            yield return For is { } difficulty ? issue.ForDifficulties(difficulty) : issue;
        }
    }

    private sealed class StubSet : BeatmapSetCheck
    {
        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            Templates(Issue.Level.Problem);

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata { Message = "Stub set." };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            yield return new Issue(GetTemplate("Template"), null, "whole set");

            foreach (var beatmap in beatmapSet.Beatmaps)
                yield return new Issue(
                    GetTemplate("Template"),
                    beatmap,
                    "set in " + beatmap.MetadataSettings.version
                );
        }
    }
}
