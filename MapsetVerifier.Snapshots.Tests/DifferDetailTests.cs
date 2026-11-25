using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots.Compare;
using MapsetVerifier.Snapshots.Diffing;
using MapsetVerifier.Snapshots.Store;
using Xunit;

namespace MapsetVerifier.Snapshots.Tests;

/// <summary> The exact difference between two versions, down to its values. </summary>
public class DifferDetailTests
{
    private const string Red = "0,500,4,2,0,100,1,0";

    private static Beatmap Build(string[] objects, string timing = Red, int mode = 0) =>
        new(
            StoreTestContext
                .BuildOsu("Test", objects, beatmapId: 1, timing: timing)
                .Replace("Mode: 0", "Mode: " + mode),
            "song",
            $"{Guid.NewGuid():N}.osu"
        );

    private static DiffResult Diff(
        string[] before,
        string[] after,
        int mode = 0,
        string timing = Red,
        string? timingAfter = null
    ) => BeatmapDiffer.Diff(Build(before, timing, mode), Build(after, timingAfter ?? timing, mode));

    private static string Circle(
        int time,
        int x = 100,
        int sound = 0,
        string sample = "0:0:0:0:"
    ) => $"{x},100,{time},1,{sound},{sample}";

    private static string Slider(int slides, string sounds, string sets) =>
        $"100,100,3000,2,0,L|240:100,{slides},140,{sounds},{sets},0:0:0:0:";

    /// <summary> Surrounded by circles which stay the same. </summary>
    private static string[] Among(string hitObject) =>
        [Circle(1000), Circle(2000), hitObject, Circle(9000), Circle(9500)];

    private static Change Only(string before, string after, int mode = 0) =>
        Assert.Single(Diff(Among(before), Among(after), mode).Changes);

    [Theory]
    [InlineData("100,100,3000,1,0,0:0:0:0:", "100,100,3000,2,0,L|240:100,1,140", "Don", "Drumroll")]
    [InlineData("100,100,3000,1,2,0:0:0:0:", "100,100,3000,2,0,L|240:100,1,140", "Kat", "Drumroll")]
    [InlineData(
        "100,100,3000,1,12,0:0:0:0:",
        "100,100,3000,12,0,4000,0:0:0:0:",
        "Big kat",
        "Shaker"
    )]
    [InlineData(
        "100,100,3000,1,4,0:0:0:0:",
        "100,100,3000,12,4,4000,0:0:0:0:",
        "Big don",
        "Shaker"
    )]
    [InlineData(
        "100,100,3000,2,4,L|240:100,1,140",
        "100,100,3000,12,4,4000,0:0:0:0:",
        "Big drumroll",
        "Shaker"
    )]
    public void Taiko_KindsOfNotes_AreNamedAsInGame(
        string before,
        string after,
        string oldKind,
        string newKind
    )
    {
        var change = Diff(Among(before), Among(after), 1).Changes.Single(c => c.Field == "Type");

        Assert.Equal((oldKind, newKind), (change.Before, change.After));
        Assert.Equal(ChangeKind.Rhythm, change.Kind);
    }

    [Theory]
    [InlineData(
        "100,100,3000,2,0,L|240:100,1,140",
        "100,100,3000,2,4,L|240:100,1,140",
        "Drumroll",
        "Big drumroll"
    )]
    [InlineData("100,100,3000,1,0,0:0:0:0:", "100,100,3000,1,8,0:0:0:0:", "Don", "Kat")]
    public void Taiko_AnotherNote_IsAPlacementChange(
        string before,
        string after,
        string oldNote,
        string newNote
    )
    {
        var change = Diff(Among(before), Among(after), 1).Changes.First();

        Assert.Equal(("Note", oldNote, newNote), (change.Field, change.Before, change.After));
    }

    [Theory]
    [InlineData(
        "100,100,3000,1,0,0:0:0:0:",
        "100,100,3000,2,0,L|240:100,1,140",
        "Circle",
        "Slider"
    )]
    [InlineData("100,100,3000,1,0,0:0:0:0:", "100,100,3000,8,0,4000,0:0:0:0:", "Circle", "Spinner")]
    public void Standard_KindsOfObjects(string before, string after, string oldKind, string newKind)
    {
        var change = Only(before, after);

        Assert.Equal(("Type", oldKind, newKind), (change.Field, change.Before, change.After));
    }

    [Theory]
    [InlineData("0|2|0", "0:0|0:0|0:0", true)] // A reverse.
    [InlineData("2|0|0", "0:0|0:0|0:0", true)] // The head.
    [InlineData("0|0|2", "0:0|0:0|0:0", true)] // The tail.
    [InlineData("0|1|0", "0:0|0:0|0:0", false)] // Plays the normal sound either way.
    [InlineData("0|0|0", "0:0|3:0|0:0", true)]
    [InlineData("0|0|0", "0:0|0:3|0:0", true)]
    [InlineData("0|0|0", "3:0|0:0|0:0", true)]
    [InlineData("0|0|0", "0:3|0:0|0:0", true)]
    [InlineData("0|0|0", "0:0|0:0|3:0", true)]
    [InlineData("0|0|0", "0:0|0:0|0:3", true)]
    [InlineData("0|0|0", "2:0|2:2|0:2", false)] // What they inherit.
    public void SliderSounds_OnlyCountIfTheyPlayDifferently(
        string sounds,
        string sets,
        bool expected
    )
    {
        var changes = Diff(
            Among(Slider(2, "0|0|0", "0:0|0:0|0:0")),
            Among(Slider(2, sounds, sets))
        ).Changes;

        Assert.Equal(expected, changes.Count == 1);
        Assert.All(
            changes,
            change =>
            {
                Assert.Equal("SliderSamples", change.Field);
                Assert.Equal(ChangeKind.Hitsound, change.Kind);
                Assert.Equal(3000, change.Time);
                Assert.Equal(4000, change.EndTime!.Value, 1);
            }
        );
    }

    [Fact]
    public void SliderWithMoreReverses_ComparesTheSoundsBothHave()
    {
        var same = Diff(
            Among(Slider(2, "0|2|0", "0:0|0:0|0:0")),
            Among(Slider(3, "0|2|8|0", "0:0|0:0|0:0|0:0"))
        ).Changes;

        var change = Assert.Single(same);
        Assert.Equal(("Reverses", "1", "2"), (change.Field, change.Before, change.After));

        Assert.Equal(
            ["Reverses", "SliderSamples"],
            Diff(
                Among(Slider(2, "0|2|0", "0:0|0:0|0:0")),
                Among(Slider(3, "0|8|8|0", "0:0|0:0|0:0|0:0"))
            )
                .Changes.Select(c => c.Field)
        );
    }

    [Theory]
    [InlineData("P|170:150|240:100", true)]
    [InlineData("L|240:120", true)]
    [InlineData("L|170:100|240:100", true)]
    [InlineData("L|240:100", false)]
    public void SliderShape(string curve, bool expected)
    {
        var changes = Diff(
            Among("100,100,3000,2,0,L|240:100,1,140"),
            Among($"100,100,3000,2,0,{curve},1,140")
        ).Changes;

        Assert.Equal(expected, changes.SingleOrDefault()?.Field == "Shape");
    }

    [Fact]
    public void SliderLength_HasItsMagnitude()
    {
        var change = Only("100,100,3000,2,0,L|240:100,1,140", "100,100,3000,2,0,L|240:100,1,70");

        Assert.Equal(("Length", "140", "70"), (change.Field, change.Before, change.After));
        Assert.Equal(70, change.Magnitude);
        Assert.Equal(3250, change.EndTime!.Value, 1);
    }

    [Theory]
    [InlineData(
        "100,100,3000,12,0,4000,0:0:0:0:",
        "100,100,3000,12,0,4001,0:0:0:0:",
        "4000",
        "4001",
        1
    )]
    [InlineData(
        "100,100,3000,12,0,4000,0:0:0:0:",
        "100,100,3000,12,0,5000,0:0:0:0:",
        "4000",
        "5000",
        1000
    )]
    public void SpinnerEnd(string before, string after, string oldEnd, string newEnd, double by)
    {
        var change = Only(before, after);

        Assert.Equal(("EndTime", oldEnd, newEnd), (change.Field, change.Before, change.After));
        Assert.Equal(by, change.Magnitude);
    }

    [Theory]
    [InlineData(103, true)]
    [InlineData(104, false)]
    public void Move_UpToThreePixels_IsMinor(int x, bool minor)
    {
        var change = Only(Circle(3000), Circle(3000, x));

        Assert.Equal("Position", change.Field);
        Assert.Equal(minor, change.Minor);
        Assert.Equal(x - 100, change.Magnitude);
        Assert.Equal(("100,100", x + ",100"), (change.Before, change.After));
    }

    [Theory]
    [InlineData(80, 77, true)]
    [InlineData(80, 76, false)]
    [InlineData(0, 3, false)] // From the line's 100.
    public void ObjectVolume_UpToThreePercent_IsMinor(int before, int after, bool minor)
    {
        var change = Only(
            Circle(3000, sample: $"0:0:0:{before}:"),
            Circle(3000, sample: $"0:0:0:{after}:")
        );

        Assert.Equal("Volume", change.Field);
        Assert.Equal(minor, change.Minor);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(125, true)]
    [InlineData(126, false)]
    public void Retime_IsOneChange_UpToALimit(int by, bool retime)
    {
        var changes = Diff(Among(Circle(3000)), Among(Circle(3000 + by))).Changes;

        if (!retime)
        {
            Assert.Equal(
                [(ChangeOp.Added, 3000d + by), (ChangeOp.Removed, 3000d)],
                changes.OrderBy(c => c.Op).Select(c => (c.Op, c.Time))
            );
            return;
        }

        var change = Assert.Single(changes);
        Assert.Equal(
            ("Time", "3000", (3000 + by).ToString()),
            (change.Field, change.Before, change.After)
        );
        Assert.Equal(by, change.Magnitude);
    }

    [Fact]
    public void ObjectsAtOneTime_PairUpByTheirPlace()
    {
        string[] chord =
        [
            Circle(1000, 64),
            Circle(1000, 192),
            Circle(1000, 320),
            Circle(1000, 448),
        ];

        // In another order they are the same, and without the last only that one is gone.
        Assert.Empty(Diff(chord, [chord[2], chord[0], chord[3], chord[1]], 3).Changes);

        var change = Assert.Single(Diff(chord, [chord[2], chord[0], chord[1]], 3).Changes);
        Assert.Equal((ChangeOp.Removed, 1000), (change.Op, change.Time));
    }

    [Fact]
    public void Mania_AMovedNote_NamesItsColumns()
    {
        var change = Only(Circle(3000, 64), Circle(3000, 320), 3);

        Assert.Equal(("Column", "1", "3"), (change.Field, change.Before, change.After));
    }

    [Theory]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-99,4,2,0,100,0,0", "Sv", true)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-50,4,2,0,100,0,0", "Sv", false)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-100,4,3,0,100,0,0", "GreenLineSampleset", false)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-100,4,2,5,100,0,0", "GreenLineSampleset", false)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-100,4,2,0,97,0,0", "GreenLineVolume", true)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-100,4,2,0,50,0,0", "GreenLineVolume", false)]
    [InlineData("1000,-100,4,2,0,100,0,0", "1000,-100,4,2,0,100,0,1", "Kiai", false)]
    [InlineData("1000,400,4,2,0,100,1,0", "1000,300,4,2,0,100,1,0", "Bpm", false)]
    [InlineData("1000,400,4,2,0,100,1,0", "1000,400,3,2,0,100,1,0", "Meter", false)]
    [InlineData("1000,400,4,2,0,100,1,0", "1000,400,4,2,0,50,1,0", "RedLineVolume", false)]
    public void LineChanges(string before, string after, string field, bool minor)
    {
        var change = Assert.Single(
            Diff(
                Among(Circle(3000)),
                Among(Circle(3000)),
                timing: Red + "|" + before,
                timingAfter: Red + "|" + after
            ).Changes
        );

        Assert.Equal(field, change.Field);
        Assert.Equal(minor, change.Minor);
        Assert.Equal(1000, change.Time);
        Assert.NotEqual(change.Before, change.After);
    }

    [Theory]
    [InlineData("1000,400,4,2,0,100,1,0", "RedLine", null)]
    [InlineData("1000,-50,4,2,0,100,0,0", "GreenLine", "2")]
    public void AddedAndRemovedLines(string line, string field, string? sv)
    {
        var objects = Among(Circle(3000));
        var added = Assert.Single(
            Diff(objects, objects, timing: Red, timingAfter: Red + "|" + line).Changes
        );
        var removed = Assert.Single(
            Diff(objects, objects, timing: Red + "|" + line, timingAfter: Red).Changes
        );

        Assert.Equal(
            (field, ChangeOp.Added, null, sv),
            (added.Field, added.Op, added.Before, added.After)
        );
        Assert.Equal(
            (field, ChangeOp.Removed, sv, null),
            (removed.Field, removed.Op, removed.Before, removed.After)
        );
    }

    private static string Volumes(params int[] volumes) =>
        string.Join(
            "|",
            volumes.Select(
                (volume, i) =>
                    i == 0 ? $"0,500,4,2,0,{volume},1,0" : $"{i * 1000},-100,4,2,0,{volume},0,0"
            )
        );

    [Theory]
    // By one amount, to one value, and both.
    [InlineData(new[] { 90, 80, 70, 60 }, new[] { 70, 60, 50, 40 }, -20d, null, null)]
    [InlineData(new[] { 90, 80, 70, 60 }, new[] { 50, 50, 50, 50 }, 0d, null, "50")]
    [InlineData(new[] { 80, 80, 80, 80 }, new[] { 50, 50, 50, 50 }, -30d, "80", "50")]
    public void VolumePass_SaysWhatItDid(
        int[] before,
        int[] after,
        double amount,
        string? from,
        string? to
    )
    {
        var objects = Among(Circle(3000));
        var result = Diff(objects, objects, timing: Volumes(before), timingAfter: Volumes(after));
        var rollup = Assert.Single(result.Rollups);

        Assert.Empty(result.Changes);
        Assert.Equal(RollupKind.Volume, rollup.Kind);
        Assert.Equal(
            (amount, 4, from, to),
            (rollup.Amount, rollup.Absorbed, rollup.Before, rollup.After)
        );
    }

    [Fact]
    public void VolumeChangesOfDifferentKinds_AreSeparateStretches()
    {
        var objects = Among(Circle(3000));
        var result = Diff(
            objects,
            objects,
            timing: Volumes(80, 80, 80, 80, 80, 80, 80, 80, 80, 80, 80),
            timingAfter: Volumes(80, 60, 60, 60, 30, 40, 80, 70, 50, 90, 80)
        );

        var group = Assert.Single(result.Changes, c => c.Field == "VolumeGroup");
        Assert.Equal((1000, 3000, 3), (group.Time, group.EndTime!.Value, group.Count!.Value));
        Assert.Equal(("80", "60", -20), (group.Before, group.After, group.Magnitude!.Value));
        Assert.Equal(5, result.Changes.Count(c => c.Field == "GreenLineVolume"));
    }

    [Fact]
    public void Shift_WithAnythingOnTop_IsReportedOnTop()
    {
        string[] Row(int by, Func<int, int> x, int end) =>
            [
                .. Enumerable.Range(0, 10).Select(i => Circle(1000 + i * 300 + by, x(i))),
                $"256,192,{5000 + by},12,0,{end},0:0:0:0:",
            ];

        var result = Diff(
            Row(0, i => 50 + i * 20, 6000),
            Row(30, i => 50 + i * 20 + (i == 4 ? 10 : 0), 6200)
        );

        Assert.Equal(30, Assert.Single(result.Rollups).Amount);
        Assert.Equal(
            [("EndTime", "6030", "6200"), ("Position", "130,100", "140,100")],
            result.Changes.OrderBy(c => c.Field).Select(c => (c.Field, c.Before, c.After))
        );
    }

    [Fact]
    public void FarMovedPattern_NeedsMoreObjects()
    {
        string[] Pattern(int start, int count) =>
            [.. Enumerable.Range(0, count).Select(i => Circle(start + i * 100, 50 + i * 60))];

        string[] fixedPart =
        [
            .. Enumerable.Range(0, 20).Select(i => Circle(20000 + i * 500, 100 + i * 5)),
        ];

        Assert.Single(
            Diff([.. Pattern(1000, 7), .. fixedPart], [.. Pattern(1900, 7), .. fixedPart]).Changes,
            c => c.Field == "Shift"
        );
        Assert.DoesNotContain(
            Diff([.. Pattern(1000, 4), .. fixedPart], [.. Pattern(1900, 4), .. fixedPart]).Changes,
            c => c.Field == "Shift"
        );
    }

    [Theory]
    [InlineData("a.osu", "Difficulty")]
    [InlineData("a.OSB", "Storyboard")]
    [InlineData("a.mp3", "Audio")]
    [InlineData("a.wav", "Audio")]
    [InlineData("a.mp4", "Video")]
    [InlineData("a.avi", "Video")]
    [InlineData("a.png", "Image")]
    [InlineData("a.txt", "Other")]
    [InlineData("a", "Other")]
    public void FileCategories(string name, string expected)
    {
        Assert.Equal(expected, FileDiffer.Category(name));
    }

    [Fact]
    public void Files_AddedRemovedAndChanged_WithKnownSizesOnly()
    {
        Dictionary<string, StoredFile> Files(
            params (string Name, string Hash, long Size)[] files
        ) => files.ToDictionary(f => f.Name, f => new StoredFile { Hash = f.Hash, Size = f.Size });

        var changes = FileDiffer.Diff(
            Files(("a.mp3", "1", 10), ("b.png", "1", 0), ("c.osu", "1", 5), ("same.png", "1", 5)),
            Files(("a.mp3", "2", 20), ("C.txt", "1", 7), ("c.osu", "2", 6), ("same.png", "1", 5))
        );

        Assert.Equal(
            [
                new FileChange("a.mp3", "Audio", ChangeOp.Changed, 10, 20),
                new FileChange("b.png", "Image", ChangeOp.Removed, null, null),
                new FileChange("C.txt", "Other", ChangeOp.Added, null, 7),
            ],
            changes
        );
    }

    private static Change At(
        double time,
        ChangeKind kind,
        string field = "Position",
        ChangeOp op = ChangeOp.Changed,
        double? end = null,
        bool minor = false
    ) =>
        new(
            kind,
            op,
            time,
            end,
            field,
            Object: new ObjectRef(time, "Circle", time.ToString()),
            Minor: minor
        );

    [Fact]
    public void Hunks_AreLabelledByWhatTheyHold()
    {
        HunkLabel[] Labels(params Change[] changes) =>
            [.. HunkBuilder.Build(changes).Select(hunk => hunk.Label)];

        Assert.Empty(Labels());
        Assert.Empty(Labels(At(0, ChangeKind.Placement, minor: true)));
        Assert.Equal([HunkLabel.Placement], Labels(At(0, ChangeKind.Placement)));
        Assert.Equal([HunkLabel.Rhythm], Labels(At(0, ChangeKind.Rhythm, "Time")));
        Assert.Equal([HunkLabel.Hitsounding], Labels(At(0, ChangeKind.Hitsound, "Hitsound")));
        Assert.Equal([HunkLabel.Sv], Labels(At(0, ChangeKind.Timing, "Sv")));
        Assert.Equal([HunkLabel.Shifted], Labels(At(0, ChangeKind.Timing, "Shift")));
        Assert.Equal([HunkLabel.Volume], Labels(At(0, ChangeKind.Hitsound, "VolumeGroup")));
        Assert.Equal(
            [HunkLabel.Mixed],
            Labels(At(0, ChangeKind.Placement), At(1, ChangeKind.Rhythm, "Time"))
        );

        // Mostly replaced.
        Assert.Equal(
            [HunkLabel.Remapped],
            Labels(
                At(0, ChangeKind.Rhythm, "Object", ChangeOp.Removed),
                At(1, ChangeKind.Rhythm, "Object", ChangeOp.Added),
                At(2, ChangeKind.Rhythm, "Object", ChangeOp.Added),
                At(3, ChangeKind.Placement)
            )
        );
        Assert.Equal(
            [HunkLabel.Rhythm],
            Labels(
                At(0, ChangeKind.Rhythm, "Object", ChangeOp.Removed),
                At(1, ChangeKind.Rhythm, "Object", ChangeOp.Added)
            )
        );
    }

    [Fact]
    public void Hunks_StartAfterTwoSecondsWithoutChanges()
    {
        var hunks = HunkBuilder.Build([
            At(0, ChangeKind.Placement, end: 3000),
            At(5000, ChangeKind.Placement),
            At(7001, ChangeKind.Placement),
        ]);

        Assert.Equal([(0, 5000), (7001, 7001)], hunks.Select(hunk => (hunk.Start, hunk.End)));
    }

    [Fact]
    public void Events_WithSeveralBackgroundsOrBreaks_AreListedEach()
    {
        static List<SettingChange> Events(string before, string after) =>
            SettingsDiffer.Diff("[Events]\n" + before, "[Events]\n" + after);

        Assert.Equal(
            [
                ("Background", ChangeOp.Removed, "a.jpg", null),
                ("Background", ChangeOp.Removed, "b.jpg", null),
                ("Background", ChangeOp.Added, null, "c.jpg"),
            ],
            Events("0,0,\"a.jpg\",0,0\n0,0,\"b.jpg\",0,0", "Background,0,\"c.jpg\",0,0")
                .Select(c => (c.Key, c.Op, c.Before, c.After))
        );
        Assert.Equal(
            [("Background", ChangeOp.Added, null, "0,0")],
            Events("", "0,0").Select(c => (c.Key, c.Op, c.Before, c.After))
        );
        // Written differently without any of its values changing.
        Assert.Equal(
            ("Background", ChangeOp.Changed),
            Events("0,0,\"a.jpg\"", "0,0,a.jpg").Select(c => (c.Key, c.Op)).Single()
        );
        Assert.Equal(
            [
                ("Break", ChangeOp.Changed, "100-200", "100-300"),
                ("Break", ChangeOp.Removed, "500-600", null),
                ("Break", ChangeOp.Added, null, "800-900"),
            ],
            Events("2,100,200\nBreak,500,600", "2,100,300\n2,800,900")
                .Select(c => (c.Key, c.Op, c.Before, c.After))
        );
        Assert.Equal(
            ("100-200", "50-200"),
            Events("2,100,200", "2,50,200").Select(c => (c.Before, c.After)).Single()
        );

        var removed = Assert.Single(Events("Sprite,Pass,Centre,\"a.png\",0,0", ""));
        Assert.Null(removed.Added);
        Assert.Equal(["1 removed"], removed.Removed!);
    }

    [Theory]
    [InlineData("General", "PreviewTime: a", "PreviewTime: 1")]
    [InlineData("General", "PreviewTime: 1", "PreviewTime: a")]
    public void ValuesWhichAreNoNumbers_AreComparedAsText(
        string section,
        string before,
        string after
    )
    {
        Assert.Single(SettingsDiffer.Diff($"[{section}]\n{before}", $"[{section}]\n{after}"));
    }
}
