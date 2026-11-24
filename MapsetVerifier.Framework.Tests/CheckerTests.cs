using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Framework.Tests;

/// <summary>
/// The registry and the audio cache the checker clears are process-wide, so this runs on its own.
/// </summary>
[CollectionDefinition(nameof(CheckerTests), DisableParallelization = true)]
public class CheckerTestsCollection;

[Collection(nameof(CheckerTests))]
public sealed class CheckerTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "MapsetVerifierFrameworkTests",
        Guid.NewGuid().ToString("N")
    );

    public CheckerTests()
    {
        Directory.CreateDirectory(SongPath);
        CheckerRegistry.ClearChecks();
    }

    private string SongPath => Path.Combine(root, "song");

    public void Dispose()
    {
        CheckerRegistry.ClearChecks();

        try
        {
            Directory.Delete(root, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private BeatmapSet Set(params (string Version, Beatmap.Mode Mode)[] difficulties)
    {
        foreach (var (version, mode) in difficulties)
            File.WriteAllText(
                Path.Combine(SongPath, version + ".osu"),
                $"""
                osu file format v14

                [General]
                Mode: {(int)mode}

                [Metadata]
                Title:Title
                Version:{version}

                [Difficulty]
                CircleSize:4

                [TimingPoints]
                0,500,4,2,0,100,1,0

                [HitObjects]
                256,192,1000,1,0,0:0:0:0:
                """
            );

        return new BeatmapSet(SongPath);
    }

    [Fact]
    public void EachKindOfCheck_RunsOnWhatItIsFor()
    {
        var general = new StubGeneralCheck();
        var standard = new StubBeatmapCheck([Beatmap.Mode.Standard]);
        var all = new StubBeatmapCheck(Enum.GetValues<Beatmap.Mode>());
        var set = new StubBeatmapSetCheck([Beatmap.Mode.Standard]);
        var maniaSet = new StubBeatmapSetCheck([Beatmap.Mode.Mania]);

        foreach (Check check in new Check[] { general, standard, all, set, maniaSet })
            CheckerRegistry.RegisterCheck(check);

        var issues = Checker.GetBeatmapSetIssues(
            Set(("Easy", Beatmap.Mode.Standard), ("Kantan", Beatmap.Mode.Taiko))
        );

        Assert.Single(issues, issue => issue.CheckOrigin == general);
        Assert.Equal("Easy", Assert.Single(issues, i => i.CheckOrigin == standard).message);
        Assert.Equal(
            ["Easy", "Kantan"],
            issues.Where(i => i.CheckOrigin == all).Select(i => i.message).Order()
        );
        // Once for the whole set, and not at all without a difficulty in one of its modes.
        Assert.Single(issues, issue => issue.CheckOrigin == set);
        Assert.DoesNotContain(issues, issue => issue.CheckOrigin == maniaSet);
    }

    [Fact]
    public void Issues_AreSortedFromTheMostSevere()
    {
        CheckerRegistry.RegisterCheck(
            new StubGeneralCheck(
                Issue.Level.Minor,
                Issue.Level.Problem,
                Issue.Level.Info,
                Issue.Level.Warning
            )
        );

        var issues = Checker.GetBeatmapSetIssues(Set(("Easy", Beatmap.Mode.Standard)));

        Assert.Equal(
            [Issue.Level.Problem, Issue.Level.Warning, Issue.Level.Minor, Issue.Level.Info],
            issues.Select(issue => issue.level)
        );
    }

    [Fact]
    public void AFailingCheck_IsNamedInTheException()
    {
        var failing = new StubGeneralCheck { Exception = new InvalidOperationException("broken") };
        CheckerRegistry.RegisterCheck(failing);

        var exception = Assert.Throws<AggregateException>(() =>
            Checker.GetBeatmapSetIssues(Set(("Easy", Beatmap.Mode.Standard)))
        );
        var inner = exception.Flatten().InnerException!;

        Assert.Equal("broken", inner.Message);
        Assert.Same(failing, inner.Data["Check"]);
    }

    [Fact]
    public void Progress_StartsAtZero_AndEndsWithEverythingCompleted()
    {
        CheckerRegistry.RegisterCheck(new StubGeneralCheck());
        CheckerRegistry.RegisterCheck(new StubBeatmapCheck(Enum.GetValues<Beatmap.Mode>()));
        CheckerRegistry.RegisterCheck(new StubBeatmapSetCheck(Enum.GetValues<Beatmap.Mode>()));
        var progress = new RecordingProgress();

        Checker.GetBeatmapSetIssues(
            Set(("Easy", Beatmap.Mode.Standard), ("Hard", Beatmap.Mode.Standard)),
            progress
        );

        // One general, one set and two difficulties.
        Assert.All(progress.Reports, report => Assert.Equal(4, report.Total));
        Assert.Equal(0, progress.Reports[0].Completed);
        Assert.Equal(4, progress.Reports.Max(report => report.Completed));
        Assert.Contains(
            progress.Reports,
            report => report.ActiveLabels.Contains("Stub beatmap check. [Hard]")
        );
    }

    [Fact]
    public void Timings_AreOnlyCollectedWhenAskedFor()
    {
        CheckerRegistry.RegisterCheck(new StubGeneralCheck());
        CheckerRegistry.RegisterCheck(new StubBeatmapCheck([Beatmap.Mode.Standard]));
        var set = Set(("Easy", Beatmap.Mode.Standard));

        Checker.GetBeatmapSetIssues(set, null, false, out var without);
        Assert.Null(without);

        Checker.GetBeatmapSetIssues(set, null, true, out var with);
        Assert.NotNull(with);
        Assert.Contains(
            with.Checks,
            t => t is { CheckName: "Stub general check.", Difficulty: null }
        );
        Assert.Contains(
            with.Checks,
            t => t is { CheckName: "Stub beatmap check.", Difficulty: "Easy" }
        );
    }

    [Fact]
    public void Registry_KeepsOrder_AndIgnoresNull()
    {
        var first = new StubGeneralCheck();
        var second = new StubBeatmapCheck([]);
        CheckerRegistry.RegisterCheck(first);
        CheckerRegistry.RegisterCheck(null);
        CheckerRegistry.RegisterCheck(second);

        Assert.Equal<Check>([first, second], CheckerRegistry.GetChecks());
        Assert.Same(second, CheckerRegistry.GetChecksWithId()[1]);
        Assert.Equal([first], CheckerRegistry.GetGeneralChecks());
        Assert.Equal([second], CheckerRegistry.GetBeatmapChecks());
        Assert.Empty(CheckerRegistry.GetBeatmapSetChecks());

        CheckerRegistry.ClearChecks();
        Assert.Empty(CheckerRegistry.GetChecks());
    }

    [Fact]
    public void CustomChecks_AreLoadedFromPlugins_AndABrokenOneIsReported()
    {
        Checker.ConfigureCustomChecksPath(root, "app");
        var plugins = Path.Combine(root, "app", "CustomChecks");
        Directory.CreateDirectory(plugins);
        // This very assembly has a check in it, so it makes for a plugin.
        File.Copy(typeof(PluginCheck).Assembly.Location, Path.Combine(plugins, "plugin.dll"));
        File.WriteAllText(Path.Combine(plugins, "broken.dll"), "not an assembly");
        File.WriteAllText(Path.Combine(plugins, "readme.txt"), "not a plugin");

        Checker.LoadCustomChecks();
        var report = Checker.GetCustomCheckPluginReport();

        Assert.True(report.CustomChecksEnabled);
        Assert.Equal(plugins, report.DirectoryPath);
        Assert.Equal("broken.dll", Assert.Single(report.FailedPlugins).FileName);

        var loaded = Assert.Single(report.LoadedPlugins);
        Assert.Equal("plugin.dll", loaded.FileName);
        Assert.Equal(1, loaded.CheckCount);
        Assert.Equal(1, loaded.GeneralCheckCount);
        Assert.Equal(0, loaded.BeatmapCheckCount);
        Assert.Equal(["Plugin check."], loaded.CheckNames);
        Assert.Equal(["Plugin author"], loaded.Authors);
        Assert.Equal("Plugin check.", Assert.Single(CheckerRegistry.GetChecks()).ToString());

        // Plugins can be replaced or removed while the app is running.
        File.Delete(Path.Combine(plugins, "plugin.dll"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReloadingChecks_StartsOver(bool loadCustomChecks)
    {
        Checker.ConfigureCustomChecksPath(root, "app");
        var plugins = Path.Combine(root, "app", "CustomChecks");
        Directory.CreateDirectory(plugins);
        File.Copy(typeof(PluginCheck).Assembly.Location, Path.Combine(plugins, "plugin.dll"));

        var report = Checker.ReloadChecks(loadCustomChecks);
        var again = Checker.ReloadChecks(loadCustomChecks);

        Assert.Equal(loadCustomChecks, report.CustomChecksEnabled);
        Assert.Equal(loadCustomChecks ? 1 : 0, report.LoadedPlugins.Count());
        Assert.Equal(loadCustomChecks ? 1 : 0, again.LoadedPlugins.Count());
        Assert.Equal(
            loadCustomChecks ? 1 : 0,
            CheckerRegistry.GetChecks().Count(check => check.ToString() == "Plugin check.")
        );
    }

    [Fact]
    public void CustomChecks_WithoutAFolder_CreatesIt()
    {
        Checker.ConfigureCustomChecksPath(root, "app");

        Checker.LoadCustomChecks();

        Assert.True(Directory.Exists(Path.Combine(root, "app", "CustomChecks")));
        Assert.Empty(Checker.GetCustomCheckPluginReport().LoadedPlugins);
        Assert.Empty(Checker.GetCustomCheckPluginReport().FailedPlugins);
    }

    [Fact]
    public void DisabledCustomChecks_AreReportedAsSuch()
    {
        Checker.ConfigureCustomChecksPath(root, "app");

        Checker.DisableCustomChecks();

        Assert.False(Checker.GetCustomCheckPluginReport().CustomChecksEnabled);
    }

    [Fact]
    public void Reloading_WithCustomChecksDisabled_LeavesPluginsOut()
    {
        Checker.ConfigureCustomChecksPath(root, "app");
        var plugins = Path.Combine(root, "app", "CustomChecks");
        Directory.CreateDirectory(plugins);
        File.Copy(typeof(PluginCheck).Assembly.Location, Path.Combine(plugins, "plugin.dll"));
        CheckerRegistry.RegisterCheck(new StubGeneralCheck());

        var report = Checker.ReloadChecks(loadCustomChecks: false);

        Assert.False(report.CustomChecksEnabled);
        Assert.Empty(report.LoadedPlugins);
        Assert.DoesNotContain(CheckerRegistry.GetChecks(), check => check is StubGeneralCheck);
        Assert.DoesNotContain(
            CheckerRegistry.GetChecks(),
            check => check.ToString() == "Plugin check."
        );
    }

    [Fact]
    public void Progress_CountsEveryHitSound()
    {
        Set(("Easy", Beatmap.Mode.Standard));

        var map = Path.Combine(SongPath, "Easy.osu");
        File.AppendAllText(map, "\n0,0,2000,1,0,0:0:0:0:a.wav\n0,0,3000,1,0,0:0:0:0:b.wav\n");
        File.WriteAllBytes(Path.Combine(SongPath, "a.wav"), [1, 2, 3]);
        File.WriteAllBytes(Path.Combine(SongPath, "b.wav"), [1, 2, 3]);

        var progress = new RecordingProgress();
        Checker.GetBeatmapSetIssues(new BeatmapSet(SongPath), progress);

        Assert.Equal(2, progress.Reports[0].Total);
        Assert.Equal(2, progress.Reports.Max(report => report.Completed));
        Assert.Contains(
            progress.Reports,
            report => report.ActiveLabels.Contains("Loading audio files")
        );
        Assert.Empty(progress.Reports[^1].ActiveLabels);
    }

    [Fact]
    public void AFailingCheck_IsStillCompleted()
    {
        CheckerRegistry.RegisterCheck(
            new StubGeneralCheck { Exception = new InvalidOperationException("broken") }
        );
        var progress = new RecordingProgress();

        Assert.Throws<AggregateException>(() =>
            Checker.GetBeatmapSetIssues(Set(("Easy", Beatmap.Mode.Standard)), progress)
        );
        Assert.Equal(1, progress.Reports[^1].Completed);
    }

    [Fact]
    public void APluginWithoutChecks_IsCreditedToItsCompany()
    {
        Checker.ConfigureCustomChecksPath(root, "external");
        var folder = Path.Combine(root, "external", "CustomChecks");
        Directory.CreateDirectory(folder);
        File.Copy(typeof(BeatmapSet).Assembly.Location, Path.Combine(folder, "parser.dll"));
        File.Copy(typeof(FactAttribute).Assembly.Location, Path.Combine(folder, "xunit.dll"));

        Checker.SuppressLoadLogging = true;

        try
        {
            Checker.LoadCustomChecks();
        }
        finally
        {
            Checker.SuppressLoadLogging = false;
        }

        var report = Checker.GetCustomCheckPluginReport();

        Assert.Equal(["parser.dll", "xunit.dll"], report.LoadedPlugins.Select(p => p.FileName));
        Assert.All(report.LoadedPlugins, plugin => Assert.Equal(0, plugin.CheckCount));
        Assert.Equal(["MapsetVerifier.Parser"], report.LoadedPlugins[0].Authors);
        Assert.NotEmpty(report.LoadedPlugins[1].Authors);
        Assert.Empty(report.FailedPlugins);
    }

    private sealed class RecordingProgress : IProgress<CheckProgress>
    {
        private readonly List<CheckProgress> reports = [];

        public List<CheckProgress> Reports
        {
            get
            {
                lock (reports)
                    return [.. reports];
            }
        }

        public void Report(CheckProgress value)
        {
            lock (reports)
                reports.Add(value);
        }
    }

    private static Dictionary<string, IssueTemplate> Templates() =>
        Enum.GetValues<Issue.Level>()
            .ToDictionary(
                level => level.ToString(),
                level => new IssueTemplate(level, "{0}", "message")
            );

    private sealed class StubGeneralCheck(params Issue.Level[] levels) : GeneralCheck
    {
        public Exception? Exception { get; init; }

        public override Dictionary<string, IssueTemplate> GetTemplates() => Templates();

        public override CheckMetadata GetMetadata() => new() { Message = "Stub general check." };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (Exception != null)
                throw Exception;

            foreach (var level in levels.DefaultIfEmpty(Issue.Level.Problem))
                yield return new Issue(GetTemplate(level.ToString()), null, "general");
        }
    }

    private sealed class StubBeatmapCheck(Beatmap.Mode[] modes) : BeatmapCheck
    {
        public override Dictionary<string, IssueTemplate> GetTemplates() => Templates();

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata { Message = "Stub beatmap check.", Modes = modes };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            yield return new Issue(
                GetTemplate("Problem"),
                beatmap,
                beatmap.MetadataSettings.version
            );
        }
    }

    private sealed class StubBeatmapSetCheck(Beatmap.Mode[] modes) : BeatmapSetCheck
    {
        public override Dictionary<string, IssueTemplate> GetTemplates() => Templates();

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata { Message = "Stub set check.", Modes = modes };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            yield return new Issue(GetTemplate("Problem"), null, "set");
        }
    }
}

/// <summary> Picked up when this assembly is loaded as a plugin, see <see cref="CheckerTests" />. </summary>
[Check]
public class PluginCheck : GeneralCheck
{
    public override Dictionary<string, IssueTemplate> GetTemplates() => [];

    public override CheckMetadata GetMetadata() =>
        new() { Message = "Plugin check.", Author = "Plugin author" };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet) => [];
}
