using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

public class SnapshotWindowTests
{
    private const string Timing = "0,500,4,2,0,100,1,0";
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static WindowResult Window(
        int mode,
        string[] objects,
        string timing = Timing,
        string extra = ""
    )
    {
        using var context = Create();
        context.WriteDifficulty(
            "a.osu",
            BuildOsu("Easy", objects, beatmapId: 1, timing: timing, extra: extra)
                .Replace("Mode: 0", "Mode: " + mode)
        );
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        return SnapshotComparer.GetWindow("set-12345", entry, entry, "b-1", 1000, 1000, 0);
    }

    [Fact]
    public void Slider_HasItsPathReversesTicksAndSounds()
    {
        // 280 px at 140 px per beat is 1000 ms per slide, with a tick in the middle of each.
        var slider = Assert.Single(
            Window(
                0,
                [
                    "100,100,1000,6,2,L|380:100,3,280,2|4|8|0,1:2|2:1|3:3|0:0,0:0:0:0:",
                    "100,300,9000,1,0,0:0:0:0:",
                ],
                extra: "SliderTickRate:1"
            ).Visual.After
        );

        Assert.Equal("Slider", slider.Type);
        Assert.Equal(4000, slider.EndTime!.Value, 2);
        Assert.Equal([2000, 3000], slider.Edges!.Select(time => Math.Round(time)));
        Assert.Equal([1500, 2500, 3500], slider.Ticks!);
        Assert.Equal(100, slider.Path![0][0]);
        Assert.Equal(380, slider.Path[^1][0]);
        Assert.Equal(
            ["Head", "Repeat", "Repeat", "Tail", "Body"],
            slider.Sounds!.Select(sound => sound.Kind)
        );
        Assert.Equal(
            ["Whistle", "Finish", "Clap", "None", "Whistle"],
            slider.Sounds!.Select(sound => sound.HitSound)
        );
        Assert.Equal(1, slider.Combo);
        Assert.True(slider.NewCombo);
    }

    [Fact]
    public void SliderWithoutEdgeSounds_HasNoneOnItsRepeats()
    {
        var slider = Assert.Single(Window(0, ["100,100,1000,2,0,L|170:100,2,70"]).Visual.After);

        Assert.Equal("None", slider.Sounds!.Single(sound => sound.Kind == "Repeat").HitSound);
        Assert.Null(slider.Ticks);
    }

    [Fact]
    public void ComboNumbers_StartOverAtNewCombosAndAfterSpinners()
    {
        var shown = Window(
            0,
            [
                "100,100,1000,1,0,0:0:0:0:",
                "100,100,1100,1,0,0:0:0:0:",
                "100,100,1200,5,0,0:0:0:0:",
                "256,192,1300,12,0,1400,0:0:0:0:",
                "100,100,1500,1,0,0:0:0:0:",
                "100,100,1600,1,0,0:0:0:0:",
            ]
        ).Visual.After;

        Assert.Equal([1, 2, 1, null, 1, 2], shown.Select(o => o.Combo));

        var spinner = shown.Single(o => o.Type == "Spinner");
        Assert.Equal(1400, spinner.EndTime);
        Assert.Null(spinner.Sounds);
    }

    [Fact]
    public void Mania_HasColumnsWithoutCombos()
    {
        var shown = Window(
            3,
            ["64,192,1000,1,0,0:0:0:0:", "448,192,1250,128,0,1500:0:0:0:0:"]
        ).Visual.After;

        Assert.Equal(2, shown.Select(o => o.Column).Distinct().Count());
        Assert.All(shown, o => Assert.NotNull(o.Column));
        Assert.All(shown, o => Assert.Null(o.NewCombo));
        Assert.All(shown, o => Assert.Null(o.Combo));
        Assert.Equal(1500, shown[1].EndTime);
    }

    [Fact]
    public void Taiko_HasOneSoundPerDrumroll()
    {
        var shown = Window(
            1,
            ["100,100,1000,1,8,0:0:0:0:", "100,100,1250,2,0,L|240:100,1,140"]
        ).Visual.After;

        Assert.All(shown, o => Assert.Equal("Head", Assert.Single(o.Sounds!).Kind));
        Assert.All(shown, o => Assert.Null(o.NewCombo));
        Assert.Null(shown[1].Ticks);
    }

    [Fact]
    public void Catch_ShowsHyperdashesAndDrops()
    {
        var shown = Window(
            2,
            [
                "0,100,1000,1,0,0:0:0:0:",
                "512,100,1100,1,0,0:0:0:0:",
                "0,100,1200,2,0,L|0:380,2,280,0|0|0,0:0|0:0|0:0,0:0:0:0:",
                "512,100,3300,1,0,0:0:0:0:",
            ],
            extra: "SliderTickRate:1"
        ).Visual.After;

        Assert.Equal([1000], shown[0].HyperTimes!);
        Assert.Equal([1100], shown[1].HyperTimes!);
        Assert.Contains(1700, shown[2].Ticks!);
        Assert.Equal(3200, Assert.Single(shown[2].HyperTimes!), 2);
    }

    [Fact]
    public void Timing_IsTheRedLinesWithinTheWindow()
    {
        var window = Window(
            0,
            Circles(8),
            "0,500,4,2,0,100,1,0|1200,400,3,2,0,100,1,0|1300,-50,4,2,0,100,0,0|60000,300,4,2,0,100,1,0"
        );

        Assert.Equal([500, 400], window.Visual.AfterTiming.Select(mark => mark.BeatLength));
        Assert.Equal(3, window.Visual.AfterTiming[1].Meter);
    }

    [Fact]
    public void WindowBeforeTheFirstRedLine_UsesThatLine()
    {
        var window = Window(0, Circles(8), "5000,400,4,2,0,100,1,0");

        Assert.Equal(1600, window.To - window.From);
        Assert.Equal(400, Assert.Single(window.Visual.AfterTiming).BeatLength);
    }

    [Fact]
    public void WithoutRedLines_BeatsAreHalfASecond()
    {
        var window = Window(0, Circles(4), "0,-100,4,2,0,100,0,0");

        Assert.Equal(2000, window.To - window.From);
        Assert.Empty(window.Visual.AfterTiming);
    }

    [Fact]
    public void AddedAndRemovedDifficulties_HaveOneSide()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        File.Delete(Path.Combine(context.SongPath, "a.osu"));
        context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(5), beatmapId: 2));
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1))!;

        var removed = SnapshotComparer.GetWindow("set-12345", before, after, "b-1", 1000, 1000, 0);
        Assert.NotEmpty(removed.Visual.Before);
        Assert.Empty(removed.Visual.After);
        Assert.Empty(removed.Visual.AfterTiming);

        var added = SnapshotComparer.GetWindow("set-12345", before, after, "b-2", 1000, 1000, 0);
        Assert.Empty(added.Visual.Before);
        Assert.Empty(added.Visual.BeforeTiming);
        Assert.NotEmpty(added.Visual.After);

        Assert.Throws<ArgumentException>(() =>
            SnapshotComparer.GetWindow("set-12345", before, after, "b-3", 1000, 1000, 0)
        );

        var result = SnapshotComparer.Compare("set-12345", before, after);
        Assert.Equal(4, result.Difficulties.Single(d => d.Key == "b-1").Counts.Removed);
        Assert.Equal(5, result.Difficulties.Single(d => d.Key == "b-2").Counts.Added);
    }

    [Fact]
    public void KiaiAndBreaks_AreShownOfTheVersionItHas()
    {
        using var context = Create();
        context.WriteDifficulty(
            "a.osu",
            BuildOsu(
                    "Easy",
                    Circles(12),
                    beatmapId: 1,
                    timing: Timing
                        + "|1000,-100,4,2,0,100,0,1|1500,-100,4,2,0,90,0,1|2000,-100,4,2,0,100,0,0|5000,-100,4,2,0,100,0,1"
                )
                .Replace("[Events]", "[Events]\n2,2700,3200")
        );
        context.WriteDifficulty(
            "b.osu",
            BuildOsu("Hard", [], beatmapId: 2, timing: Timing + "|700,-100,4,2,0,100,0,1")
        );
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        var result = SnapshotComparer.Compare("set-12345", entry, entry);

        var easy = result.Difficulties.Single(d => d.Key == "b-1");
        Assert.Equal([(1000, 2000), (5000, 6500)], easy.Kiai.Select(k => (k.Start, k.End)));
        Assert.Equal((2700, 3200), easy.Breaks.Select(b => (b.Start, b.End)).Single());

        var empty = result.Difficulties.Single(d => d.Key == "b-2");
        Assert.Equal([(700, 700)], empty.Kiai.Select(k => (k.Start, k.End)));
        Assert.Equal(0, empty.LengthMs);
    }

    [Fact]
    public void TheSameVolumePassEverywhere_IsOneGeneralRollup()
    {
        string Lines(int volume) =>
            string.Join(
                "|",
                Enumerable
                    .Range(0, 8)
                    .Select(i =>
                        $"{i * 1000},{(i == 0 ? 500 : -100)},4,2,0,{volume},{(i == 0 ? 1 : 0)},0"
                    )
            );

        using var context = Create();

        void Write(int volume)
        {
            context.WriteDifficulty(
                "a.osu",
                BuildOsu("Easy", Circles(8), beatmapId: 1, timing: Lines(volume))
            );
            context.WriteDifficulty(
                "b.osu",
                BuildOsu("Hard", Circles(9, x: 80), beatmapId: 2, timing: Lines(volume))
            );
        }

        Write(80);
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        Write(60);
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1))!;

        var result = SnapshotComparer.Compare("set-12345", before, after);

        Assert.Single(result.General.Rollups);
        Assert.All(result.Difficulties, d => Assert.Empty(d.Rollups));
    }

    [Fact]
    public void ChangesFarApart_AreMarkedApart()
    {
        using var context = Create();
        var objects = Circles(20);
        context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        foreach (var i in new[] { 2, 3, 15 })
            objects[i] = objects[i].Replace(",100,", ",300,");

        context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1))!;

        var easy = Assert.Single(SnapshotComparer.Compare("set-12345", before, after).Difficulties);

        Assert.Equal([2000, 2500, 8500], easy.Marks.Select(m => m.Start));
    }

    [Fact]
    public void ChangesRightAfterEachOther_AreOneMark()
    {
        using var context = Create();
        var objects = Circles(20, step: 100);
        context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        for (var i = 2; i < 6; i++)
            objects[i] = objects[i].Replace(",100,", ",300,");

        context.WriteDifficulty("a.osu", BuildOsu("Easy", objects, beatmapId: 1));
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(1))!;

        var mark = Assert.Single(
            Assert.Single(SnapshotComparer.Compare("set-12345", before, after).Difficulties).Marks
        );

        Assert.Equal((1200, 1500), (mark.Start, mark.End));
    }

    /// <summary> The beat grid starts with the line it's under, also if that's the very first thing in it. </summary>
    [Fact]
    public void Timing_IsOfTheLinesShapingTheWindow()
    {
        var timing = Window(
            0,
            Circles(40, start: 1000, step: 500, x: 10),
            "0,500,4,2,0,100,1,0|800,400,3,2,0,100,1,0|900,-50,4,2,0,100,0,0|900000,300,4,2,0,100,1,0"
        ).Visual.AfterTiming;

        Assert.DoesNotContain(timing, mark => mark.Offset is 900 or 900000);
        Assert.Contains(timing, mark => mark is { Offset: 800, BeatLength: 400, Meter: 3 });
    }
}
