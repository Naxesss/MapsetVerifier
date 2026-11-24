using System.Text.Json;
using MapsetVerifier.Framework;
using MapsetVerifier.Logging;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Sync;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace MapsetVerifier.Server.Tests;

/// <summary> A copy of the ranking criteria data, which the working directory is in until disposed. </summary>
public sealed class RankingCriteriaCheckout : IDisposable
{
    private readonly SongFolder folder = new();
    private readonly string workingDirectory = Directory.GetCurrentDirectory();

    public RankingCriteriaCheckout()
    {
        var source = RcDataDirectory.Find();
        folder.Write("MapsetVerifier.slnx");
        DataPath = Path.Combine(folder.Path, "MapsetVerifier.RankingCriteria", "Data");

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(DataPath, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }

        Directory.SetCurrentDirectory(folder.Path);
        Assert.Equal(new DirectoryInfo(DataPath).FullName, RcDataDirectory.Find());
    }

    public string DataPath { get; }

    public string ConstantsPath => Path.Combine(DataPath, "..", "RC.g.cs");

    public void Dispose()
    {
        Directory.SetCurrentDirectory(workingDirectory);
        folder.Dispose();
    }

    public RcStatement MarkOutdated()
    {
        var page = RcCatalogueFiles.ReadPage(DataPath, "catch");
        var statement = page.Statements.First(statement => !statement.IsRetired);
        statement.LastReview = new RcReview { Commit = "0123456789abcdef", Kind = RcKind.Rule };
        RcCatalogueFiles.WritePage(DataPath, page);

        return statement;
    }

    public RcStatement Read(string id) =>
        RcCatalogueFiles
            .ReadPages(DataPath)
            .SelectMany(page => page.Statements)
            .Single(statement => statement.Id == id);
}

/// <summary> What maintains the ranking criteria, from the command line or a Debug build. </summary>
public sealed class RankingCriteriaToolTests : IDisposable
{
    private readonly RankingCriteriaCheckout checkout = new();
    private readonly TextWriter output = Console.Out;
    private readonly TextWriter errors = Console.Error;

    public void Dispose()
    {
        Console.SetOut(output);
        Console.SetError(errors);
        checkout.Dispose();
    }

    private static async Task<(int Code, string Output, string Errors)> Run(params string[] args)
    {
        var written = new StringWriter();
        var failed = new StringWriter();
        Console.SetOut(written);
        Console.SetError(failed);

        var code = await RankingCriteria.Tool.Program.Main(args);

        return (code, written.ToString(), failed.ToString());
    }

    [Fact]
    public async Task Reparse_ChangesNothing_IfNothingChanged()
    {
        var before = Directory
            .GetFiles(Path.Combine(checkout.DataPath, "catalogue"))
            .ToDictionary(file => file, File.ReadAllText);

        var (code, written, errors) = await Run("reparse");

        Assert.Equal((0, ""), (code, written));
        Assert.Contains("catch: ", errors);
        Assert.All(
            before,
            file =>
                Assert.Equal(
                    file.Value,
                    File.ReadAllText(file.Key),
                    ignoreLineEndingDifferences: true
                )
        );
        Assert.Contains("class RC", File.ReadAllText(checkout.ConstantsPath));
    }

    [Fact]
    public async Task Reparse_ReportsWhatChanged()
    {
        // As if its page was never there.
        File.Delete(Path.Combine(checkout.DataPath, "catalogue", "catch.json"));

        var (code, written, _) = await Run("reparse");

        Assert.Equal(0, code);
        Assert.Matches(@"## \w+ \(\d+\)", written);
        Assert.Contains("- `catch/", written);
    }

    [Fact]
    public async Task Reparse_FailsWithoutTheSnapshot()
    {
        Directory.Delete(Path.Combine(checkout.DataPath, "snapshots"), true);

        var (code, _, errors) = await Run("reparse");

        Assert.Equal(1, code);
        Assert.StartsWith("Error: ", errors);
    }

    [Fact]
    public async Task Review_ListsWhatIsWaiting_AndMarksIt()
    {
        var statement = checkout.MarkOutdated();

        var (code, written, _) = await Run("review");
        Assert.Equal(0, code);
        Assert.Contains($"{statement.Id} (changed since 01234567): ", written);

        (code, _, var errors) = await Run("review", statement.Id);
        Assert.Equal(0, code);
        Assert.Contains($"Reviewed {statement.Id}", errors);
        Assert.Null(checkout.Read(statement.Id).LastReview);

        (code, _, errors) = await Run("review", statement.Id);
        Assert.Equal(1, code);
        Assert.Contains($"{statement.Id} is not waiting for a review.", errors);
    }

    [Theory]
    [InlineData("help")]
    [InlineData("help", "--commit")]
    [InlineData("help", "--commit", "abc")]
    public async Task UnknownCommand_ShowsTheUsage(params string[] args)
    {
        var (code, _, errors) = await Run(args);

        Assert.Equal(1, code);
        Assert.StartsWith("Usage: ", errors);
    }

#if DEBUG
    private readonly Server.Controller.RankingCriteriaCurationController controller = new();

    private const string Unknown = "catch/not-a-statement";

    [Theory]
    [InlineData("  Because. ", "Because.")]
    [InlineData(" ", null)]
    public void Curation_IsWrittenToTheCheckout(string notes, string? expected)
    {
        var page = RcCatalogueFiles.ReadPage(checkout.DataPath, "catch");
        var id = page.Statements.First(statement => !statement.IsRetired).Id;

        RankingCriteriaService.Store = new RcStore(
            new RcSource(),
            new Dictionary<string, string>(),
            new Dictionary<string, RcCataloguePage> { ["catch"] = page }
        );

        try
        {
            var result = controller.UpdateCuration(new(id, RcAutomation.Automatable, notes));

            Assert.IsType<OkObjectResult>(result.Result);
            var written = checkout.Read(id).Curation;
            Assert.Equal((RcAutomation.Automatable, expected), (written.Automation, written.Notes));

            Assert.IsType<NotFoundObjectResult>(
                controller.UpdateCuration(new(Unknown, RcAutomation.Automatable, null)).Result
            );
        }
        finally
        {
            RankingCriteriaService.Store = RcStore.Embedded;
        }
    }

    [Fact]
    public void Curation_OfAStatementNotInTheCheckout_Fails()
    {
        RankingCriteriaService.Store = new RcStore(
            new RcSource(),
            new Dictionary<string, string>(),
            new Dictionary<string, RcCataloguePage>
            {
                ["catch"] = new()
                {
                    Page = "catch",
                    Statements = [new RcStatement { Id = Unknown }],
                },
            }
        );

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                controller.UpdateCuration(new(Unknown, RcAutomation.Automatable, null))
            );
        }
        finally
        {
            RankingCriteriaService.Store = RcStore.Embedded;
        }
    }

    [Fact]
    public void Review_IsWrittenToTheCheckout()
    {
        var id = checkout.MarkOutdated().Id;

        RankingCriteriaService.Store = new RcStore(
            new RcSource(),
            new Dictionary<string, string>(),
            new Dictionary<string, RcCataloguePage>
            {
                ["catch"] = RcCatalogueFiles.ReadPage(checkout.DataPath, "catch"),
            }
        );

        try
        {
            Assert.IsType<OkObjectResult>(controller.MarkReviewed(new(id)).Result);
            Assert.Null(checkout.Read(id).LastReview);
            Assert.IsType<NotFoundObjectResult>(controller.MarkReviewed(new(Unknown)).Result);
        }
        finally
        {
            RankingCriteriaService.Store = RcStore.Embedded;
        }
    }
#endif
}

/// <summary> What writes the checks for the website. </summary>
public sealed class ExportTests : IDisposable
{
    private readonly SongFolder folder = new();

    public ExportTests() => CheckerRegistry.ClearChecks();

    public void Dispose()
    {
        Checker.SuppressLoadLogging = false;
        CheckerRegistry.ClearChecks();
        folder.Dispose();
    }

    [Fact]
    public void EveryCheckAndStatement_IsExported()
    {
        var path = Path.Combine(folder.Path, "new", "checks.json");

        Assert.Equal(0, Exports.Program.Main([path]));

        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var checks = json.RootElement.GetProperty("checks").EnumerateArray().ToList();
        Assert.Equal(CheckerRegistry.GetChecks().Count, checks.Count);
        Assert.Equal(
            ["Beatmap", "BeatmapSet", "General"],
            checks.Select(check => check.GetProperty("checkType").GetString()).Distinct().Order()
        );
        Assert.All(
            checks,
            check =>
            {
                Assert.NotEmpty(check.GetProperty("templates").EnumerateObject());
                Assert.All(
                    check.GetProperty("documentation").EnumerateObject(),
                    text => Assert.DoesNotMatch("(?m)^ {4}", text.Value.GetString())
                );
            }
        );

        var criteria = json.RootElement.GetProperty("rankingCriteria");
        var pages = criteria
            .GetProperty("pages")
            .EnumerateArray()
            .Select(page => page.GetProperty("key").GetString())
            .ToList();
        Assert.All(
            criteria.GetProperty("statements").EnumerateArray(),
            statement => Assert.Contains(statement.GetProperty("page").GetString(), pages)
        );
    }

    [Fact]
    public void UnwritablePath_IsAnError()
    {
        Assert.Equal(1, Exports.Program.Main([folder.Path]));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" \n ", "")]
    [InlineData("a", "a")]
    [InlineData("\n\n    a\n      b  \n\n    c\n  \n", "a\n  b\n\nc")]
    [InlineData("\n    a\n\n- b\n      - c", "a\n\n- b\n  - c")]
    [InlineData("a\n    b", "a\n    b")]
    [InlineData("\r\n  a\r\n  b\r", "a\nb")]
    public void Documentation_LosesTheIndentationOfTheSource(string text, string expected)
    {
        Assert.Equal(expected, Exports.Program.CleanDocumentationText(text));
    }
}

public sealed class LoggingTests : IDisposable
{
    private readonly SongFolder folder = new();
    private readonly ILogger previous = Log.Logger;

    public void Dispose()
    {
        Log.Logger = previous;
        folder.Dispose();
    }

    private sealed class Factory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructure) =>
            new(name, new ScalarValue(value));
    }

    private static object? Shorten(LogEventPropertyValue? source)
    {
        var entry = new LogEvent(
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            source == null ? [] : [new LogEventProperty("SourceContext", source)]
        );
        new ShortSourceContextEnricher().Enrich(entry, new Factory());

        return ((ScalarValue)entry.Properties["ShortSourceContext"]).Value;
    }

    [Theory]
    [InlineData("MapsetVerifier.Server.Service.BeatmapService", "M.S.S.BeatmapService")]
    [InlineData("Program", "Program")]
    [InlineData("A..B", "A..B")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Source_IsShortened(string? source, string expected)
    {
        Assert.Equal(expected, Shorten(new ScalarValue(source)));
    }

    [Fact]
    public void Source_MayBeMissing_OrNotBeText()
    {
        Assert.Equal("main", Shorten(null));
        Assert.Equal("[\"a\"]", Shorten(new SequenceValue([new ScalarValue("a")])));
    }

    [Fact]
    public void Log_IsWrittenToTheGivenFolder()
    {
        LoggerConfigurator.Configure(folder.Path);
        Log.ForContext<LoggingTests>().Information("Written.");
        Log.CloseAndFlush();

        var file = Assert.Single(
            Directory.GetFiles(Path.Combine(folder.Path, "Mapset Verifier Externals", "Logs"))
        );
        Assert.Contains("MapsetVerifier M.S.T.LoggingTests Written.", File.ReadAllText(file));
    }

    [Fact]
    public void Log_FailsIfItCannotBeWritten()
    {
        var file = folder.Write("Mapset Verifier Externals", "not a folder");

        Assert.ThrowsAny<IOException>(() =>
            LoggerConfigurator.Configure(Path.GetDirectoryName(file)!)
        );
    }
}
