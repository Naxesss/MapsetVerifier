using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;
using Xunit;
using static MapsetVerifier.Snapshots.Tests.StoreTestContext;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary> What exactly a comparison says about one object, line or set. </summary>
public class ChangeDetailTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private const string Timing = "0,500,4,2,0,100,1,0";
    private const string BaseSlider = "50,100,3000,2,0,L|190:100,1,140,0|0,0:0|0:0,0:0:0:0:";

    private static ComparisonResult Compare(
        Action<StoreTestContext> before,
        Action<StoreTestContext> after,
        out SnapshotEntry first,
        out SnapshotEntry second
    )
    {
        using var context = Create();
        before(context);
        first = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        after(context);
        second = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(5))!;

        return SnapshotComparer.Compare("set-12345", first, second);
    }

    private static List<Change> Changes(
        string[] before,
        string[] after,
        string timingBefore = Timing,
        string timingAfter = Timing,
        int mode = 0
    )
    {
        string Osu(string[] objects, string timing) =>
            BuildOsu("Hard", objects, beatmapId: 1, timing: timing)
                .Replace("Mode: 0", $"Mode: {mode}");

        var difficulty = Assert.Single(
            Compare(
                context => context.WriteDifficulty("a.osu", Osu(before, timingBefore)),
                context => context.WriteDifficulty("a.osu", Osu(after, timingAfter)),
                out _,
                out _
            ).Difficulties
        );

        return difficulty.Hunks.SelectMany(hunk => hunk.Changes).Concat(difficulty.Minor).ToList();
    }

    /// <summary> Circles with the given object among them. </summary>
    private static string[] With(string hitObject) =>
        [.. Circles(4), hitObject, .. Circles(4, 5000)];

    private static Change Single(string before, string after, int mode = 0) =>
        Assert.Single(Changes(With(before), With(after), mode: mode));

    [Theory]
    [InlineData(
        "300,100,3000,1,0,0:0:0:0:",
        "300,100,3000,1,0,3:0:0:0:",
        "Sampleset",
        "Auto/Auto",
        "Drum/Auto"
    )]
    [InlineData("300,100,3000,1,0,0:0:0:0:", "300,100,3000,1,0,0:0:5:0:", "CustomIndex", "0", "5")]
    [InlineData("300,100,3000,1,0,0:0:0:80:", "300,100,3000,1,0,0:0:0:40:", "Volume", "80", "40")]
    [InlineData(
        "300,100,3000,1,0,0:0:0:0:",
        "300,100,3000,1,0,0:0:0:0:a.wav",
        "Filename",
        null,
        "a.wav"
    )]
    public void HitSoundSettings_OfAnObject(
        string before,
        string after,
        string field,
        string? oldValue,
        string? newValue
    )
    {
        var change = Single(before, after);

        Assert.Equal(ChangeKind.Hitsound, change.Kind);
        Assert.Equal(ChangeOp.Changed, change.Op);
        Assert.Equal(field, change.Field);
        Assert.Equal(oldValue, change.Before);
        Assert.Equal(newValue, change.After);
        Assert.Equal(3000, change.Time);
    }

    [Fact]
    public void ValuesSetToWhatTheyInherit_AreNoChange()
    {
        // The line has the soft sampleset and full volume.
        var same = Changes(
            With("300,100,3000,1,0,0:0:0:0:"),
            [.. With("300,100,3000,1,0,2:0:0:100:"), "300,100,9000,1,0,0:0:0:0:"]
        );

        Assert.Equal(ChangeOp.Added, Assert.Single(same).Op);
    }

    [Theory]
    [InlineData("50,100,3000,2,0,L|190:100,1,70,0|0,0:0|0:0,0:0:0:0:", "Length", ChangeKind.Rhythm)]
    [InlineData(
        "50,100,3000,2,0,L|190:100,2,140,0|0|0,0:0|0:0|0:0,0:0:0:0:",
        "Reverses",
        ChangeKind.Rhythm
    )]
    [InlineData(
        "50,100,3000,2,0,L|50:240,1,140,0|0,0:0|0:0,0:0:0:0:",
        "Shape",
        ChangeKind.Placement
    )]
    [InlineData(
        "50,100,3000,2,0,L|190:100,1,140,8|0,0:0|0:0,0:0:0:0:",
        "SliderSamples",
        ChangeKind.Hitsound
    )]
    [InlineData(
        "50,100,3000,2,0,L|190:100,1,140,0|2,0:0|0:0,0:0:0:0:",
        "SliderSamples",
        ChangeKind.Hitsound
    )]
    [InlineData(
        "50,100,3000,2,0,L|190:100,1,140,0|0,3:0|0:0,0:0:0:0:",
        "SliderSamples",
        ChangeKind.Hitsound
    )]
    [InlineData(
        "50,100,3000,2,0,L|190:100,1,140,0|0,0:0|0:3,0:0:0:0:",
        "SliderSamples",
        ChangeKind.Hitsound
    )]
    public void SliderField(string after, string field, ChangeKind kind)
    {
        var change = Single(BaseSlider, after);

        Assert.Equal(field, change.Field);
        Assert.Equal(kind, change.Kind);
    }

    [Fact]
    public void SliderSoundsThatPlayTheSame_AreNoChange()
    {
        // Normal plays either way, and the line already is soft.
        Assert.Empty(
            Changes(With(BaseSlider), With("50,100,3000,2,0,L|190:100,1,140,1|0,2:0|0:0,0:0:0:0:"))
        );
    }

    [Fact]
    public void SpinnerAndHoldNote_ReportTheirEnd()
    {
        var spinner = Single("256,192,3000,12,0,4000,0:0:0:0:", "256,192,3000,12,0,4250,0:0:0:0:");
        Assert.Equal(("EndTime", "4000", "4250"), (spinner.Field, spinner.Before, spinner.After));

        var hold = Single(
            "64,192,3000,128,0,4000:0:0:0:0:",
            "64,192,3000,128,0,4500:0:0:0:0:",
            mode: 3
        );
        Assert.Equal(("EndTime", "4000", "4500"), (hold.Field, hold.Before, hold.After));
    }

    [Fact]
    public void Mania_ReportsColumns_RatherThanPositions()
    {
        var change = Single("64,192,3000,1,0,0:0:0:0:", "320,192,3000,1,0,0:0:0:0:", mode: 3);

        Assert.Equal(("Column", "1", "3"), (change.Field, change.Before, change.After));
        Assert.Empty(
            Changes(With("64,192,3000,1,0,0:0:0:0:"), With("70,50,3000,1,0,0:0:0:0:"), mode: 3)
        );
    }

    [Theory]
    [InlineData("0,500,4,2,0,100,1,1", "Kiai", "False", "True")]
    [InlineData("0,500,3,2,0,100,1,0", "Meter", "4", "3")]
    [InlineData("0,500,4,3,2,100,1,0", "RedLineSampleset", "Soft/0", "Drum/2")]
    [InlineData("0,500,4,2,0,60,1,0", "RedLineVolume", "100", "60")]
    [InlineData("0,400,4,2,0,100,1,0", "Bpm", "120", "150")]
    public void RedLine(string after, string field, string oldValue, string newValue)
    {
        var change = Assert.Single(
            Changes(Circles(4), Circles(4), Timing, after),
            candidate => candidate.Field == field
        );

        Assert.Equal((oldValue, newValue), (change.Before, change.After));
    }

    [Fact]
    public void SmallVolumeChange_IsMinor()
    {
        var change = Assert.Single(Changes(Circles(4), Circles(4), Timing, "0,500,4,2,0,98,1,0"));

        Assert.True(change.Minor);
    }

    [Fact]
    public void AddedAndRemovedDifficulties_AreNotCompared()
    {
        var result = Compare(
            context =>
            {
                context.WriteDifficulty("a.osu", BuildOsu("Easy", Circles(4), beatmapId: 1));
                context.WriteDifficulty("b.osu", BuildOsu("Hard", Circles(6), beatmapId: 2));
            },
            context =>
            {
                File.Delete(Path.Combine(context.SongPath, "b.osu"));
                context.WriteDifficulty("c.osu", BuildOsu("Insane", Circles(8), beatmapId: 3));
            },
            out _,
            out _
        );
        var difficulties = result.Difficulties.ToDictionary(difficulty => difficulty.Name);

        Assert.Equal(DifficultyStatus.Unchanged, difficulties["Easy"].Status);
        Assert.Equal(DifficultyStatus.Removed, difficulties["Hard"].Status);
        Assert.Equal(
            (6, 0),
            (difficulties["Hard"].ObjectsBefore, difficulties["Hard"].ObjectsAfter)
        );
        Assert.Equal(DifficultyStatus.Added, difficulties["Insane"].Status);
        Assert.Equal(8, difficulties["Insane"].ObjectsAfter);
        Assert.Empty(difficulties["Insane"].Hunks);
    }

    [Fact]
    public void KiaiAndBreaks_AreThoseOfTheNewVersion()
    {
        var difficulty = Assert.Single(
            Compare(
                context =>
                    context.WriteDifficulty("a.osu", BuildOsu("Hard", Circles(4), beatmapId: 1)),
                context =>
                    context.WriteDifficulty(
                        "a.osu",
                        BuildOsu(
                                "Hard",
                                Circles(4),
                                beatmapId: 1,
                                timing: Timing
                                    + "|1000,-100,4,2,0,100,0,1|2000,-100,4,2,0,100,0,0|2400,-100,4,2,0,100,0,1"
                            )
                            .Replace("[TimingPoints]", "2,1200,1800\n[TimingPoints]")
                    ),
                out _,
                out _
            ).Difficulties
        );

        // The last one lasts until the last object.
        Assert.Equal([new TimeRange(1000, 2000), new TimeRange(2400, 2500)], difficulty.Kiai);
        Assert.Single(difficulty.Breaks);
    }

    [Fact]
    public void Window_ShowsCombosTicksAndSounds()
    {
        string[] Objects(int x) =>
            [
                "50,100,1000,5,0,0:0:0:0:",
                "100,100,1500,1,0,0:0:0:0:",
                $"{x},100,2000,6,2,L|380:100,1,280,8|0,0:0|0:0,0:0:0:0:",
                "256,192,3500,1,0,0:0:0:0:",
            ];
        using var context = Create();
        context.WriteDifficulty(
            "a.osu",
            BuildOsu("Hard", Objects(100), beatmapId: 1, extra: "SliderTickRate:1")
        );
        var before = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;
        context.WriteDifficulty(
            "a.osu",
            BuildOsu("Hard", Objects(120), beatmapId: 1, extra: "SliderTickRate:1")
        );
        var after = SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(5))!;
        var key = after.Difficulties[0].Key;

        var window = SnapshotComparer.GetWindow("set-12345", before, after, key, 2000, 3000, 0);
        var slider = Assert.Single(window.Visual.After, o => o.Type == "Slider");

        Assert.Equal(1, slider.Combo);
        Assert.Equal([2500], slider.Ticks!);
        Assert.Equal(["Head", "Tail", "Body"], slider.Sounds!.Select(sound => sound.Kind));
        Assert.Equal("Clap", slider.Sounds![0].HitSound);
        Assert.Equal("Whistle", slider.Sounds![2].HitSound);
        Assert.Equal(120, slider.X);
        Assert.Equal(100, Assert.Single(window.Visual.Before, o => o.Type == "Slider").X);
        Assert.Equal(2, Assert.Single(window.Visual.After, o => o.Time.Equals(1500)).Combo);
    }

    [Fact]
    public void History_FillsInStarsAndChecks()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Hard", Circles(20, step: 200), beatmapId: 1));
        SnapshotStore.Capture(context.Load(), "manual", now: T0);
        SnapshotStore.AttachChecks(
            "set-12345",
            new StoredChecks
            {
                Problems = 1,
                Warnings = 2,
                Minor = 3,
            }
        );

        context.WriteDifficulty("a.osu", BuildOsu("Hard", Circles(21, step: 200), beatmapId: 1));
        SnapshotStore.Capture(context.Load(), "manual", now: T0.AddMinutes(5));
        var history = SnapshotHistory.Build("set-12345");

        Assert.Equal(2, history.Entries.Count);
        Assert.Null(history.Entries[0].Checks);
        Assert.Equal(2, history.Entries[0].PreviousChecks!.Warnings);
        Assert.Equal(1, history.Entries[0].Counts.Added);
        Assert.Equal(1, history.Entries[1].Checks!.Problems);
        Assert.True(history.Entries[1].IsFirst);
        Assert.True(Assert.Single(history.Difficulties).StarRating > 0);
    }

    [Fact]
    public void StoredStars_AreNotReplaced()
    {
        using var context = Create();
        context.WriteDifficulty("a.osu", BuildOsu("Hard", Circles(4), beatmapId: 1));
        var entry = SnapshotStore.Capture(context.Load(), "manual", now: T0)!;

        SnapshotStore.SaveStars(
            "set-12345",
            new Dictionary<string, double> { { entry.Difficulties[0].Blob, 4.2 } }
        );

        Assert.Equal(
            entry.Difficulties[0].Stars,
            SnapshotStore.Load("set-12345").Entries[0].Difficulties[0].Stars
        );
    }
}
