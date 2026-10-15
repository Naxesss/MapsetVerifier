using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Objects;
using MapsetVerifier.Snapshots.Translators;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotAlignmentTests
{
    [Theory]
    [InlineData(600)]
    [InlineData(600.000005)]
    public void AlignmentPreservesExactAndNearlyEqualMatchTies(double cost)
    {
        var path = SequenceAlignment.Align(
            new[] { 0 },
            new[] { 0 },
            time => time,
            (_, _) => cost,
            5000
        );
        Assert.Equal(Tuple.Create(0, 0), Assert.Single(path));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 2)]
    [InlineData(2, 0)]
    public void AlignmentHandlesEmptyInputs(int oldCount, int newCount)
    {
        var path = SequenceAlignment.Align(
            Enumerable.Range(0, oldCount).ToArray(),
            Enumerable.Range(0, newCount).ToArray(),
            time => time,
            (_, _) => 0,
            5000
        );
        Assert.Equal(oldCount + newCount, path.Count);
        Assert.All(path, pair => Assert.True(pair.Item1 == -1 || pair.Item2 == -1));
    }

    [Theory]
    [InlineData(5000, 1)]
    [InlineData(5001, 2)]
    public void AlignmentHonorsTheTimeWindow(int newTime, int expectedSteps)
    {
        var path = SequenceAlignment.Align(
            new[] { 0 },
            new[] { newTime },
            time => time,
            (_, _) => 0,
            5000
        );
        Assert.Equal(expectedSteps, path.Count);
    }

    [Fact]
    public void CacheDoesNotReuseResultsFromAnotherVersionOfTheMap()
    {
        var oldCode = BuildOsu(0);
        var firstMap = new Beatmap(BuildOsu(10), "", "first.osu");
        var secondMap = new Beatmap(BuildOsu(20), "", "second.osu");

        var first = HitObjectsTranslator.GetCachedShiftSections(firstMap, oldCode, null);
        var second = HitObjectsTranslator.GetCachedShiftSections(secondMap, oldCode, null);

        Assert.Equal(10, first.globalShiftHint);
        Assert.Equal(20, second.globalShiftHint);
        Assert.Same(
            first.sections,
            HitObjectsTranslator.GetCachedShiftSections(firstMap, oldCode, null).sections
        );
    }

    [Fact]
    public void CacheUpdatesWhenTheHistoricalComparisonChanges()
    {
        var oldCode = BuildOsu(0);
        var beatmap = new Beatmap(oldCode, "", "test.osu");

        var first = HitObjectsTranslator.GetCachedShiftSections(beatmap, oldCode, BuildOsu(10));
        var second = HitObjectsTranslator.GetCachedShiftSections(beatmap, oldCode, BuildOsu(20));

        Assert.Equal(10, first.globalShiftHint);
        Assert.Equal(20, second.globalShiftHint);
    }

    [Fact]
    public void AlignmentKeepsInsertionsAndRemovalsAroundShiftedObjects()
    {
        var oldCode = BuildOsu(0);
        var newCode =
            BuildOsu(10).Replace("100,100,3010,1,0,0:0:0:0:\n", "") + "200,200,9000,1,0,0:0:0:0:\n";
        var beatmap = new Beatmap(newCode, "", "test.osu");

        var result = HitObjectsTranslator.GetShiftSections(beatmap, oldCode, newCode);
        var steps = result.sections.SelectMany(section => section.Steps).ToList();

        Assert.Equal(4, steps.Count(step => step.OldObj != null && step.NewObj != null));
        Assert.Equal(3000, Assert.Single(steps, step => step.NewObj == null).OldObj!.time);
        Assert.Equal(9000, Assert.Single(steps, step => step.OldObj == null).NewObj!.time);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(5, 20)]
    [InlineData(6, 20)]
    public void TimingAndPositionEditsDoNotReportAnObjectTimeShift(int objectCount, int timingShift)
    {
        var oldCode = BuildOsu(0, objectCount);
        var newCode = oldCode
            .Replace("0,500,4,2", $"{timingShift},500,4,2")
            .Replace("100,100,1000,", "120,100,1000,");
        var beatmap = new Beatmap(newCode, "", "test.osu");
        beatmap.MetadataSettings.beatmapSetId = 123;
        beatmap.MetadataSettings.beatmapId = 456;
        var creationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var path = Path.Combine(
            Path.GetTempPath(),
            "MapsetVerifierSnapshotsTests",
            Guid.NewGuid().ToString("N")
        );
        try
        {
            Snapshotter.ConfigurePath(path, "test");
            var directory = Path.Combine(path, "test", "snapshots", "123", "456");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "2026-01-01 00-00-00.osu"), oldCode);
            var diff = new DiffInstance(
                "120,100,1000,1,0,0:0:0:0:",
                "HitObjects",
                Snapshotter.DiffType.Added,
                new List<string>(),
                creationDate
            );

            var result = new HitObjectsTranslator().Translate(new[] { diff }, beatmap).ToList();

            Assert.DoesNotContain(
                result,
                item => item.Diff.Contains("Global object offset shifted")
            );
            Assert.Contains(
                result,
                item => item.Details.Any(detail => detail.Contains("Moved from"))
            );
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    private static string BuildOsu(int shift, int count = 5) =>
        "osu file format v14\n[General]\nMode: 0\n"
        + "[Metadata]\nTitle:Test\nArtist:Test\nCreator:Test\nVersion:Test\n"
        + "[Difficulty]\nCircleSize:4\nSliderMultiplier:1.4\nSliderTickRate:1\n"
        + "[TimingPoints]\n0,500,4,2,0,100,1,0\n[HitObjects]\n"
        + string.Concat(
            Enumerable
                .Range(1, count)
                .Select(index => $"100,100,{index * 1000 + shift},1,0,0:0:0:0:\n")
        );
}
