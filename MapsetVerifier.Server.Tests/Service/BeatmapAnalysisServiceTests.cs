using MapsetVerifier.Server.Model.BeatmapAnalysis;
using MapsetVerifier.Server.Service;
using Xunit;

namespace MapsetVerifier.Server.Tests.Service;

public sealed class BeatmapAnalysisServiceTests : IDisposable
{
    private readonly SongFolder song = new();

    public void Dispose() => song.Dispose();

    private ObjectsOverviewDifficulty Objects(string osu)
    {
        song.Write("a.osu", osu);
        var result = BeatmapAnalysisService.AnalyzeObjects(song.Path);
        Assert.True(result.Success, result.ErrorMessage);

        return Assert.Single(result.Difficulties);
    }

    private static Dictionary<string, int> Types(ObjectsOverviewDifficulty difficulty) =>
        difficulty.ObjectTypes.ToDictionary(type => type.Label, type => type.Count);

    [Fact]
    public void EmptyOrMissingFolder_IsAnError()
    {
        var missing = System.IO.Path.Combine(song.Path, "missing");

        Assert.Equal(
            "No beatmaps found in folder.",
            BeatmapAnalysisService.Analyze(song.Path).ErrorMessage
        );
        Assert.False(BeatmapAnalysisService.AnalyzeObjects(song.Path).Success);
        Assert.False(BeatmapAnalysisService.AnalyzeDifficulty(song.Path).Success);
        Assert.False(BeatmapAnalysisService.Analyze(missing).Success);
        Assert.False(BeatmapAnalysisService.AnalyzeObjects(missing).Success);
        Assert.False(BeatmapAnalysisService.AnalyzeDifficulty(missing).Success);
    }

    [Fact]
    public void Statistics_OfAStandardDifficulty()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                timingPoints:
                [
                    "0,500,4,2,0,100,1,0",
                    "2000,-100,4,2,0,100,0,1",
                    "4000,-100,4,2,0,100,0,0",
                    "9000,-100,4,2,0,100,0,1",
                ],
                events: ["2,3200,7000"],
                hitObjects:
                [
                    "0,100,1000,6,0,L|200:100,1,140,0|0,0:0|0:0,0:0:0:0:",
                    "256,192,3000,1,0,0:0:0:0:",
                    "256,192,8000,5,0,0:0:0:0:",
                    "256,192,9000,12,0,10000,0:0:0:0:",
                ]
            )
        );

        var result = BeatmapAnalysisService.Analyze(song.Path);
        var stats = Assert.Single(result.Statistics);

        Assert.Equal(2, stats.CircleCount);
        Assert.Equal(1, stats.SliderCount);
        Assert.Equal(1, stats.SpinnerCount);
        Assert.Null(stats.HoldNoteCount);
        Assert.Null(stats.ObjectsPerColumn);
        Assert.Equal(3, stats.NewComboCount);
        Assert.Equal(1, stats.BreakCount);
        Assert.Equal(1, stats.UninheritedLineCount);
        Assert.Equal(3, stats.InheritedLineCount);
        Assert.Equal(9000, stats.PlayTimeMs);
        // 3000 to 8000 is a break.
        Assert.Equal(4000, stats.DrainTimeMs);
        // From 2000 to 4000, and from 9000 to the end of the last object.
        Assert.Equal(3000, stats.KiaiTimeMs);
    }

    [Fact]
    public void Statistics_OfAManiaDifficulty_AreByColumn()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                mode: 3,
                hitObjects:
                [
                    "64,192,1000,1,0,0:0:0:0:",
                    "64,192,2000,1,0,0:0:0:0:",
                    "448,192,2000,128,0,3000:0:0:0:0:",
                ]
            )
        );

        var stats = Assert.Single(BeatmapAnalysisService.Analyze(song.Path).Statistics);

        Assert.Equal(2, stats.CircleCount);
        Assert.Equal(1, stats.HoldNoteCount);
        Assert.Null(stats.SliderCount);
        Assert.Equal(4, stats.ColumnCount);
        Assert.Equal([2, 0, 0, 1], stats.ObjectsPerColumn!);
    }

    [Fact]
    public void Settings_LeaveOutWhatDoesNotApplyToTheMode()
    {
        song.Write("a.osu", SongFolder.Osu("Standard"));
        song.Write("b.osu", SongFolder.Osu("Taiko", mode: 1));
        song.Write("c.osu", SongFolder.Osu("Mania", mode: 3));

        var result = BeatmapAnalysisService.Analyze(song.Path);
        var settings = result.DifficultySettings.ToDictionary(s => s.Version);

        Assert.Equal("4", settings["Standard"].CircleSize);
        Assert.Equal("7", settings["Standard"].ApproachRate);
        Assert.Equal("1.4", settings["Standard"].SliderVelocity);
        Assert.Null(settings["Taiko"].CircleSize);
        Assert.Equal("4", settings["Mania"].CircleSize);
        Assert.Null(settings["Mania"].ApproachRate);
        Assert.Null(settings["Mania"].SliderVelocity);

        var general = result.GeneralSettings.ToDictionary(s => s.Version);
        Assert.Equal("7", general["Standard"].StackLeniency);
        Assert.Null(general["Taiko"].StackLeniency);
    }

    [Fact]
    public void GeneralSettings_OnlyMentionStoryboardSettingsWithAStoryboard()
    {
        song.Write("a.osu", SongFolder.Osu("Without", general: ["EpilepsyWarning: 1"]));
        song.Write(
            "b.osu",
            SongFolder.Osu(
                "With",
                general: ["EpilepsyWarning: 1", "UseSkinSprites: 1"],
                events: ["Sprite,Pass,Centre,\"a.png\",0,0"]
            )
        );

        var settings = BeatmapAnalysisService
            .Analyze(song.Path)
            .GeneralSettings.ToDictionary(s => s.Version);

        Assert.Null(settings["Without"].EpilepsyWarning);
        Assert.Null(settings["Without"].UseSkinSprites);
        Assert.Equal("True", settings["With"].EpilepsyWarning);
        Assert.Equal("True", settings["With"].UseSkinSprites);
    }

    [Theory]
    [InlineData("Countdown: 0", 10000, false, false)]
    [InlineData("Countdown: 2", 20000, true, false)]
    [InlineData("Countdown: 2", 3000, false, true)]
    public void Countdown_NeedsRoomBeforeTheFirstObject(
        string setting,
        int firstObject,
        bool has,
        bool insufficient
    )
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(general: [setting], hitObjects: [$"256,192,{firstObject},5,0,0:0:0:0:"])
        );

        var settings = Assert.Single(BeatmapAnalysisService.Analyze(song.Path).GeneralSettings);

        Assert.Equal(has, settings.HasCountdown);
        Assert.Equal(insufficient, settings.CountdownInsufficientTime);
    }

    [Fact]
    public void Difficulties_AreOrderedByModeThenStars()
    {
        string[] Stream(int count, int spacing) =>
            Enumerable
                .Range(0, count)
                .Select(i => $"{i % 2 * spacing},100,{1000 + i * 125},1,0,0:0:0:0:")
                .ToArray();

        song.Write("a.osu", SongFolder.Osu("Z", hitObjects: Stream(50, 50)));
        song.Write("b.osu", SongFolder.Osu("A", hitObjects: Stream(50, 400)));
        song.Write("c.osu", SongFolder.Osu("M", mode: 3, hitObjects: Stream(50, 400)));

        Assert.Equal(
            ["Z", "A", "M"],
            BeatmapAnalysisService.Analyze(song.Path).Statistics.Select(s => s.Version)
        );
        Assert.Equal(
            ["Z", "A", "M"],
            BeatmapAnalysisService.AnalyzeDifficulty(song.Path).Difficulties.Select(d => d.Version)
        );
    }

    [Fact]
    public void Objects_AreCountedByTypeInEachMode()
    {
        Assert.Equal(
            new Dictionary<string, int> { { "Circles", 2 }, { "Spinners", 1 } },
            Types(
                Objects(
                    SongFolder.Osu(
                        hitObjects:
                        [
                            "256,192,1000,5,0,0:0:0:0:",
                            "256,192,2000,1,0,0:0:0:0:",
                            "256,192,3000,12,0,4000,0:0:0:0:",
                        ]
                    )
                )
            )
        );
        Assert.Equal(
            new Dictionary<string, int>
            {
                { "Dons", 1 },
                { "Kats", 2 },
                { "Big Dons", 1 },
                { "Big Kats", 1 },
            },
            Types(
                Objects(
                    SongFolder.Osu(
                        mode: 1,
                        hitObjects: new[] { 0, 2, 8, 4, 12 }
                            .Select((sound, i) => $"256,192,{1000 + i * 500},1,{sound},0:0:0:0:")
                            .ToArray()
                    )
                )
            )
        );
        Assert.Equal(
            new Dictionary<string, int>
            {
                { "Fruits", 1 },
                { "Slider heads", 1 },
                { "Slider repeats", 1 },
                { "Slider tails", 1 },
                { "Drops", 2 },
            },
            Types(
                Objects(
                    SongFolder.Osu(
                        mode: 2,
                        hitObjects:
                        [
                            "256,192,500,5,0,0:0:0:0:",
                            "0,100,1000,2,0,L|380:100,2,280,0|0|0,0:0|0:0|0:0,0:0:0:0:",
                        ]
                    )
                )
            )
        );
        Assert.Equal(
            new Dictionary<string, int> { { "Notes", 1 }, { "Hold Notes", 1 } },
            Types(
                Objects(
                    SongFolder.Osu(
                        mode: 3,
                        hitObjects: ["64,192,1000,1,0,0:0:0:0:", "192,192,2000,128,0,3000:0:0:0:0:"]
                    )
                )
            )
        );
    }

    [Fact]
    public void Edges_AreCountedBySnapping()
    {
        var difficulty = Objects(
            SongFolder.Osu(
                hitObjects:
                [
                    "256,192,1000,5,0,0:0:0:0:", // 1/1
                    "256,192,1750,1,0,0:0:0:0:", // 1/2
                    "256,192,2166,1,0,0:0:0:0:", // 1/3
                    "256,192,2810,1,0,0:0:0:0:", // Unsnapped
                    "0,100,3000,2,0,L|200:100,1,70,0|0,0:0|0:0,0:0:0:0:", // 1/1 and 1/2
                ]
            )
        );
        var snappings = difficulty.Snappings.ToDictionary(s => s.Divisor, s => s.Count);

        Assert.Equal(6, difficulty.EdgeCount);
        Assert.Equal(2, snappings[1]);
        Assert.Equal(2, snappings[2]);
        Assert.Equal(1, snappings[3]);
        Assert.Equal(1, difficulty.UnsnappedCount);
        Assert.Equal([2810], difficulty.UnsnappedEdgeTimesMs);
        Assert.Equal(100d / 6, difficulty.UnsnappedPercentage, 6);
    }

    [Fact]
    public void Spinners_HaveNoSnapping()
    {
        // Spinners usually are a new combo.
        Assert.Equal(
            1,
            Objects(
                SongFolder.Osu(
                    hitObjects: ["256,192,1000,5,0,0:0:0:0:", "256,192,2000,12,0,3000,0:0:0:0:"]
                )
            ).EdgeCount
        );
    }

    [Fact]
    public void Timeline_CoversAllDifficulties_WithSomeRoom()
    {
        song.Write("a.osu", SongFolder.Osu("A"));
        song.Write("b.osu", SongFolder.Osu("B", hitObjects: ["256,192,5000,12,0,9000,0:0:0:0:"]));

        var result = BeatmapAnalysisService.AnalyzeObjects(song.Path);

        Assert.Equal(-2000, result.StartTimeMs);
        Assert.Equal(11000, result.EndTimeMs);
    }

    [Fact]
    public void TimingSegments_LastUntilTheNextRedLine()
    {
        var segments = Objects(
            SongFolder.Osu(
                timingPoints:
                [
                    "0,500,4,2,0,100,1,0",
                    "1000,-50,4,2,0,100,0,0",
                    "2000,400,3,2,0,100,1,0",
                ]
            )
        ).TimingSegments;

        Assert.Equal([0, 2000], segments.Select(segment => segment.StartTimeMs));
        Assert.Equal(2000, segments[0].EndTimeMs);
        Assert.Equal(120, segments[0].Bpm, 6);
        Assert.Equal(3, segments[1].Meter);
    }

    [Fact]
    public void Columns_AreCountedByNotesAndHoldNotes()
    {
        var columns = Objects(
            SongFolder.Osu(
                mode: 3,
                hitObjects:
                [
                    "64,192,1000,1,0,0:0:0:0:",
                    "64,192,2000,128,0,2500:0:0:0:0:",
                    "320,192,3000,1,0,0:0:0:0:",
                    "320,192,4000,1,0,0:0:0:0:",
                ]
            )
        ).ColumnUsage!;

        Assert.Equal([1, 2, 3, 4], columns.Select(column => column.Column));
        Assert.Equal([1, 0, 2, 0], columns.Select(column => column.NoteCount));
        Assert.Equal([1, 0, 0, 0], columns.Select(column => column.HoldNoteCount));
        Assert.Equal([50d, 0, 50, 0], columns.Select(column => column.Percentage));
    }

    [Fact]
    public void ComboColours_AreThoseInTheFile_AndDonsAndKatsInTaiko()
    {
        var standard = Objects(SongFolder.Osu(colours: ["Combo1 : 255,0,0", "Combo2 : 0,255,10"]));
        // The first new combo is the second colour.
        Assert.All(
            standard.TimelineObjects,
            hitObject => Assert.Equal("#00FF0A", hitObject.ComboColourHex)
        );

        var taiko = Objects(
            SongFolder.Osu(
                mode: 1,
                hitObjects: ["256,192,1000,1,0,0:0:0:0:", "256,192,2000,1,8,0:0:0:0:"]
            )
        );
        Assert.Equal(
            ["#EB452C", "#448DAB"],
            taiko.TimelineObjects.Select(hitObject => hitObject.ComboColourHex)
        );
        Assert.All(taiko.TimelineObjects, hitObject => Assert.Null(hitObject.ComboColourIndex));
    }

    [Fact]
    public void SliderEdges_HaveTheirOwnHitSounds()
    {
        var slider = Assert.Single(
            Objects(
                SongFolder.Osu(
                    hitObjects: ["0,100,1000,2,2,L|200:100,2,140,8|4|0,0:0|0:0|0:0,0:0:0:0:"]
                )
            ).TimelineObjects
        );

        Assert.Equal(
            ["Slider head", "Slider reverse", "Slider tail"],
            slider.Edges.Select(edge => edge.PartName)
        );
        Assert.Equal([8, 4, 0], slider.Edges.Select(edge => edge.HitSoundFlags));
        Assert.Equal(2, slider.SliderBodyHitSoundFlags);
    }

    [Fact]
    public void DifficultyCharts_AreSampledEvenly_UpToTheEnd()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                timingPoints: ["0,500,4,2,0,100,1,0", "2000,-50,4,2,0,60,0,0"],
                hitObjects: Enumerable
                    .Range(0, 40)
                    .Select(i => $"{i % 2 * 300},100,{1000 + i * 200},1,0,0:0:0:0:")
                    .ToArray()
            )
        );

        var result = BeatmapAnalysisService.AnalyzeDifficulty(song.Path);
        var difficulty = Assert.Single(result.Difficulties);

        Assert.Equal(400, result.MsPerPeak);
        Assert.Equal(0, difficulty.StarRatingSamples[0].TimeMs);
        Assert.Equal(400, difficulty.StarRatingSamples[1].TimeMs);
        Assert.True(difficulty.StarRatingSamples[^1].TimeMs >= 8800);
        Assert.Equal(difficulty.StarRating, difficulty.StarRatingSamples[^1].Value, 6);

        Assert.Equal(
            [(0d, 1d), (2000, 2), (8800, 2)],
            difficulty.SliderVelocitySamples.Select(point => (point.TimeMs, point.Value))
        );
        Assert.Equal(
            [(0d, 100d), (2000, 60), (8800, 60)],
            difficulty.VolumeSamples.Select(point => (point.TimeMs, point.Value))
        );

        Assert.NotEmpty(difficulty.Skills);
        Assert.All(
            difficulty.Skills,
            skill => Assert.Equal(difficulty.StarRatingSamples.Count, skill.StrainSamples.Count)
        );
    }

    [Fact]
    public void LongMaps_AreSampledLessOften()
    {
        song.Write(
            "a.osu",
            SongFolder.Osu(
                hitObjects: ["256,192,1000,5,0,0:0:0:0:", "256,192,3600000,5,0,0:0:0:0:"]
            )
        );

        var result = BeatmapAnalysisService.AnalyzeDifficulty(song.Path);

        Assert.True(result.MsPerPeak > 400);
        Assert.Equal(0, result.MsPerPeak % 50);
        Assert.True(Assert.Single(result.Difficulties).StarRatingSamples.Count <= 1501);
    }

    [Fact]
    public void DifficultiesOfTheSameName_InDifferentModes_AreBothCharted()
    {
        song.Write("a.osu", SongFolder.Osu("Normal"));
        song.Write("b.osu", SongFolder.Osu("Normal", mode: 3));

        var result = BeatmapAnalysisService.AnalyzeDifficulty(song.Path);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(2, result.Difficulties.Count);
    }
}
