using System.Text;
using MapsetVerifier.Checks.AllModes.General.Storyboard;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.AllModes.Storyboard;

public class StoryboardChecksTests
{
    // The only object of the difficulty is at 1000 ms.

    [Theory]
    [InlineData(" F,0,0,6900,1", false)] // 2 second song, with 5 seconds leniency
    [InlineData(" F,0,0,7500,1", true)]
    [InlineData(" L,0,80|  F,0,0,100,1", true)]
    [InlineData(" L,0,60|  F,0,0,100,1", false)]
    [InlineData(" T,HitSound,0,9000|  F,0,0,9000,1", false)]
    public void AfterSong(string commands, bool isReported)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", Osu("Centre", 320, 240, commands).AudioFilename("a.wav").Build())],
            extraBinaryFiles: [("a.wav", SilentWave(2000))]
        );

        AssertReported(
            context.RunGeneralCheck<CheckStoryboardAfterSong>(),
            isReported,
            Issue.Level.Warning
        );
    }

    [Theory]
    [InlineData(" F,0,1000,500,1,0", true)]
    [InlineData(" F,0,500,1000,1,0", false)]
    [InlineData(" F,0,500,,1", false)]
    [InlineData(" P,0,1000,500,A", true)]
    [InlineData(" L,0,2|  F,0,1000,500,1,0", true)]
    public void Backwards(string commands, bool isReported) =>
        AssertReported(
            Run<CheckStoryboardConflicts>(commands)
                .Where(issue => issue.message.Contains("end before they start")),
            isReported,
            Issue.Level.Warning
        );

    [Theory]
    [InlineData(" T,HitSoundClap,500,1500|  F,0,0,100,1,0", false)]
    [InlineData(" T,HitSoundSoftDrumClap6,500,1500|  F,0,0,100,1,0", false)]
    [InlineData(" T,HitSound|  F,0,0,100,1,0", false)]
    [InlineData(" T,Passing,5000,6000|  F,0,0,100,1,0", false)]
    [InlineData(" T,HitSoundClap,2000,3000|  F,0,0,100,1,0", true)] // no objects
    [InlineData(" T,HitSoundClap,1500,500|  F,0,0,100,1,0", true)]
    [InlineData(" T,Clap,500,1500|  F,0,0,100,1,0", true)]
    public void ImpossibleTrigger(string commands, bool isReported) =>
        AssertReported(Run<CheckStoryboardConflicts>(commands), isReported, Issue.Level.Warning);

    [Theory]
    [InlineData(" F,0,0,1000,0,1| F,0,500,1500,1,0", true)]
    [InlineData(" F,0,0,1000,0,1| F,0,500,,1", true)]
    [InlineData(" F,0,0,1000,0,1| F,0,0,1000,0,1", true)]
    [InlineData(" F,0,0,1000,0,1| F,0,1000,1500,1,0", false)]
    [InlineData(" F,0,0,1000,0,1| F,0,998,1500,1,0", false)] // rounding
    [InlineData(" F,0,0,,1| F,0,0,1000,1,0| F,0,1000,,0", false)] // sets a value at the edges
    [InlineData(" F,0,0,1000,0,1| S,0,500,1500,1,0", false)]
    [InlineData(" P,0,0,1000,A| P,0,500,1500,H", false)]
    [InlineData(" L,0,3|  F,0,0,1000,0,1| F,0,2500,2600,1", true)]
    [InlineData(" L,0,3|  F,0,0,1000,0,1| F,0,3000,3600,1", false)]
    [InlineData(" F,0,0,1000,0,1| T,HitSound,0,5000|  F,0,0,1000,1,0", false)]
    [InlineData(" T,HitSound,0,5000|  F,0,0,1000,1,0|  F,0,500,1000,1,0", true)]
    public void Overlap(string commands, bool isReported) =>
        AssertReported(Run<CheckStoryboardConflicts>(commands), isReported, Issue.Level.Minor);

    [Theory]
    [InlineData(15, 6, true)]
    [InlineData(15, 5, false)] // 4 intervals
    [InlineData(16, 20, false)]
    [InlineData(0, 20, false)]
    public void Dense(int interval, int amount, bool isReported) =>
        AssertReported(
            Run<CheckStoryboardDenseCommands>(
                string.Join(
                    '|',
                    Enumerable.Range(0, amount).Select(i => $" F,0,{i * interval},,{i % 2}")
                )
            ),
            isReported,
            Issue.Level.Minor
        );

    [Theory]
    [InlineData("0 10 20 30 40 50", true)]
    [InlineData("0 1000 1010 1020 1030 1040 1050 2000", true)] // in between others
    [InlineData("50 40 30 20 10 0", true)] // not in order
    [InlineData("0 10 10 20 30 30 40 50", true)] // same time within the run
    [InlineData("0 10 10 10 20 30 40", false)] // same time doesn't count
    [InlineData("0 10 500 510 1000 1010 1500 1510 2000 2010 2500 2510", false)] // pairs
    [InlineData("0 15 37 52 74 89 111 126 148 163", false)] // alternating 15 and 22
    [InlineData("0 10 20 30 40 56 66 76 86 96", false)] // two runs that are too short
    public void DenseRuns(string times, bool isReported) =>
        AssertReported(
            Run<CheckStoryboardDenseCommands>(
                string.Join('|', times.Split(' ').Select((time, i) => $" F,0,{time},,{i % 2}"))
            ),
            isReported,
            Issue.Level.Minor
        );

    [Fact]
    public void DenseRuns_OfDifferentTypes_AreSeparate() =>
        AssertReported(
            Run<CheckStoryboardDenseCommands>(
                " F,0,0,,0| S,0,10,,1| F,0,20,,1| S,0,30,,2| F,0,40,,0| S,0,50,,1| F,0,60,,1"
            ),
            false,
            Issue.Level.Minor
        );

    [Theory]
    [InlineData(" MX,0,0,1000,0,100| MY,0,0,1000,0,100", true)]
    [InlineData(" L,0,2|  MX,0,0,1000,0,100|  MY,0,0,1000,0,100", true)]
    [InlineData(" MX,1,0,1000,0,100| MY,0,0,1000,0,100", false)]
    [InlineData(" MX,0,0,1000,0,100| MY,0,0,1500,0,100", false)]
    [InlineData(" L,0,2|  MX,0,0,1000,0,100| MY,0,0,1000,0,100", false)]
    public void SplitMoves(string commands, bool isReported) =>
        AssertReported(Run<CheckStoryboardSplitMoves>(commands), isReported, Issue.Level.Minor);

    [Theory]
    [InlineData(8, 500, 500, 0, true)]
    [InlineData(8, 500, 400, 0, true)] // gaps
    [InlineData(7, 500, 500, 0, false)]
    [InlineData(8, 500, 600, 0, false)] // overlaps, which loops cannot
    [InlineData(8, 500, 400, 1, false)] // irregular
    public void Repeats(int amount, int period, int duration, int drift, bool isReported) =>
        AssertReported(
            Run<CheckStoryboardRepeats>(
                string.Join(
                    '|',
                    Enumerable
                        .Range(0, amount)
                        .Select(i => i * period + i * i * drift)
                        .Select(time => $" F,0,{time},{time + duration},1,0")
                )
            ),
            isReported,
            Issue.Level.Minor
        );

    [Fact]
    public void Repeats_FindsPatternsOfMultipleCommands()
    {
        var commands = Enumerable
            .Range(0, 8)
            .Select(i =>
                $" F,0,{i * 500},{i * 500 + 200},0,1| F,0,{i * 500 + 200},{i * 500 + 500},1,0"
            );

        var issue = Assert.Single(Run<CheckStoryboardRepeats>(string.Join('|', commands)));

        // 16 commands become a loop with 2 commands.
        Assert.StartsWith("13 storyboard", issue.message);
    }

    [Theory]
    [InlineData("Centre", 320, 240, " F,0,0,1000,1", false)]
    [InlineData("Centre", 800, 240, " F,0,0,1000,1", true)]
    [InlineData("Centre", 790, 240, " F,0,0,1000,1", false)] // the image is 100 wide
    [InlineData("TopLeft", 790, 240, " F,0,0,1000,1", true)]
    [InlineData("Centre", 790, 240, " S,0,0,1000,0.5", true)]
    [InlineData("Centre", 800, 240, " S,0,0,1000,0.5,2", false)]
    [InlineData("Centre", 800, 240, " R,0,0,1000,0,1", false)]
    [InlineData("Centre", 320, 600, " F,0,0,1000,1", true)]
    [InlineData("Centre", 900, 240, " MX,0,0,1000,900,700", false)]
    [InlineData("Centre", 320, 240, " MX,0,0,1000,900,950", true)]
    [InlineData("Centre", 900, 240, " M,0,0,1000,320,240", false)]
    [InlineData("Centre", 900, 240, " MX,25,0,1000,900,820", false)] // elastic goes further
    [InlineData("Centre", 900, 240, " F,0,0,1000,1| T,HitSound,0,1000|  MX,0,0,1000,700", false)]
    [InlineData("Centre", 900, 240, " T,HitSound,0,1000|  F,0,0,1000,1", false)]
    public void OffScreen(string origin, int x, int y, string commands, bool isReported)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [("test.osu", Osu(origin, x, y, commands).Build())],
            extraBinaryFiles: [("sb/a.png", TestMedia.Png(100, 100))]
        );

        AssertReported(
            context.RunGeneralCheck<CheckStoryboardOffScreen>(),
            isReported,
            Issue.Level.Warning
        );
    }

    [Fact]
    public void OffScreen_IgnoresMissingImages() =>
        Assert.Empty(Run<CheckStoryboardOffScreen>(" M,0,0,1000,2000,2000"));

    [Theory]
    [InlineData(" T,HitSound,0,5000|  F,0,0,100,0,1", true)]
    [InlineData(" T,HitSound,0,5000|  S,0,0,100,0,1", true)]
    [InlineData(" T,HitSound,0,5000|  F,0,0,100,1,0", false)]
    [InlineData(" T,HitSound,0,5000|  S,0,0,100,1,0", false)]
    [InlineData(" T,Passing,0,5000|  F,0,0,100,0,1| T,Failing,0,5000|  F,0,0,100,1,0", false)]
    [InlineData(" F,0,5000,5500,1,0| T,HitSound,0,5000|  F,0,0,100,0,1", false)]
    [InlineData(" F,0,0,,0| T,HitSound,0,5000|  F,0,0,100,0,1", true)] // hidden too early
    [InlineData(" F,0,0,,0| T,HitSound,0,5000|  S,0,0,100,1,2", false)] // never shown
    [InlineData(" F,0,0,1000,0,1", false)]
    public void Triggers(string commands, bool isReported) =>
        AssertReported(Run<CheckStoryboardTriggers>(commands), isReported, Issue.Level.Warning);

    [Fact]
    public void Osb_IsIncluded()
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Artist("Artist")
                .Title("Title")
                .Creator("Creator")
                .WithDefaultTiming()
                .WithDefaultHitObject(),
            extraFileContents:
            [
                (
                    "Artist - Title (Creator).osb",
                    "[Events]\nSprite,Foreground,Centre,\"sb/a.png\",320,240\n MX,0,0,9,0,1\n MY,0,0,9,0,1\n"
                ),
            ]
        );

        var issue = Assert.Single(context.RunGeneralCheck<CheckStoryboardSplitMoves>());

        Assert.Null(issue.beatmap);
    }

    [Fact]
    public void Issues_ListAFewSprites()
    {
        var sprites = Enumerable
            .Range(0, 5)
            .SelectMany(i =>
                new[]
                {
                    $"Sprite,Foreground,Centre,\"sb/{i}.png\",320,240",
                    " MX,0,1500,2000,0,1",
                    " MY,0,1500,2000,0,1",
                }
            );

        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder().Events(sprites).WithDefaultTiming().WithDefaultHitObject()
        );

        var issue = Assert.Single(context.RunGeneralCheck<CheckStoryboardSplitMoves>());

        Assert.StartsWith("5 ", issue.message);
        Assert.Contains("\"sb/0.png\" at 00:01:500, ", issue.message);
        Assert.EndsWith("\"sb/2.png\" at 00:01:500 and 2 more.", issue.message);
    }

    private static OsuBuilder Osu(string origin, int x, int y, string commands) =>
        new OsuBuilder()
            .Events([$"Sprite,Foreground,{origin},\"sb/a.png\",{x},{y}", .. commands.Split('|')])
            .WithDefaultTiming()
            .WithDefaultHitObject();

    private static List<Issue> Run<TCheck>(string commands)
        where TCheck : GeneralCheck, new()
    {
        using var context = CheckTestContext.CreateFromOsu(Osu("Centre", 320, 240, commands));

        return context.RunGeneralCheck<TCheck>();
    }

    private static void AssertReported(
        IEnumerable<Issue> issues,
        bool isReported,
        Issue.Level level
    )
    {
        if (isReported)
            Assert.Equal(level, Assert.Single(issues).level);
        else
            Assert.Empty(issues);
    }

    private static byte[] SilentWave(int milliseconds)
    {
        const int sampleRate = 8000;
        var size = sampleRate * milliseconds / 1000 * 2;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII);

        writer.Write("RIFF"u8);
        writer.Write(36 + size);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(size);
        writer.Write(new byte[size]);
        writer.Flush();

        return stream.ToArray();
    }

    [Theory]
    [InlineData("sb/a.png", 1, "1 sprite is")]
    [InlineData("sb/a.png", 2, "2 sprites are")]
    [InlineData("../a.png", 1, null)]
    [InlineData("", 1, null)]
    [InlineData("sb/broken.png", 1, null)]
    public void OffScreen_CountsSpritesWithAReadableImage(string path, int count, string? expected)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .Events(
                            Enumerable
                                .Range(0, count)
                                .SelectMany(_ =>
                                    new[]
                                    {
                                        $"Sprite,Foreground,Centre,\"{path}\",2000,2000",
                                        " F,0,0,1000,1",
                                    }
                                )
                        )
                        .WithDefaultTiming()
                        .WithDefaultHitObject()
                        .Build()
                ),
            ],
            extraFiles: ["sb/placeholder.txt"],
            extraBinaryFiles: [("sb/a.png", TestMedia.Png(50, 50)), ("sb/broken.png", [1, 2, 3])]
        );

        var issue = context.RunGeneralCheck<CheckStoryboardOffScreen>().SingleOrDefault();

        if (expected == null)
            Assert.Null(issue);
        else
            Assert.Contains(expected, issue!.message);
    }

    [Fact]
    public void OffScreen_LooksAtEveryFrameOfAnAnimation()
    {
        bool IsReported(int width)
        {
            using var context = CheckTestContext.CreateFromOsuFiles(
                [
                    (
                        "test.osu",
                        new OsuBuilder()
                            .Events(
                                "Animation,Foreground,Centre,\"sb/a.png\",1000,240,2,100,LoopForever",
                                " F,0,0,1000,1"
                            )
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    ),
                ],
                extraFiles: ["sb/placeholder.txt"],
                extraBinaryFiles:
                [
                    ("sb/a0.png", TestMedia.Png(50, 50)),
                    ("sb/a1.png", TestMedia.Png(width, 50)),
                ]
            );

            return context.RunGeneralCheck<CheckStoryboardOffScreen>().Count > 0;
        }

        Assert.True(IsReported(50));
        Assert.False(IsReported(1000));
    }

    [Theory]
    [InlineData(1, "1 sprite is")]
    [InlineData(2, "2 sprites are")]
    public void AfterSong_OfTheStoryboard_IsAfterTheLongestSong(int count, string expected)
    {
        string Sprites(int end) =>
            string.Concat(
                Enumerable.Repeat(
                    $"\nSprite,Foreground,Centre,\"a.png\",320,240\n F,0,0,{end},1",
                    count
                )
            );

        List<Issue> RunWith(int end)
        {
            using var context = CheckTestContext.CreateFromOsuFiles(
                new[] { ("a", "short.wav"), ("b", "long.wav") }.Select(x =>
                    (
                        x.Item1 + ".osu",
                        new OsuBuilder()
                            .Version(x.Item1)
                            .AudioFilename(x.Item2)
                            .WithDefaultTiming()
                            .WithDefaultHitObject()
                            .Build()
                    )
                ),
                extraFileContents:
                [
                    ("MapsetVerifier - MapsetVerifier (Tests).osb", "[Events]" + Sprites(end)),
                ],
                extraBinaryFiles: [("short.wav", SilentWave(1000)), ("long.wav", SilentWave(3000))]
            );

            return context.RunGeneralCheck<CheckStoryboardAfterSong>();
        }

        Assert.Empty(RunWith(7900));
        Assert.Contains(expected, Assert.Single(RunWith(8600)).message);
    }
}
