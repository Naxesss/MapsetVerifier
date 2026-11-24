using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Exceptions;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

/// <summary> Files with something wrong in them either say what can't be read, or are read and checked to the end. </summary>
public class DamagedMapTests
{
    private const string Map = """
        osu file format v14

        [General]
        PreviewTime: 1000
        Countdown: 1
        StackLeniency: 0.7
        Mode: MODE

        [Metadata]
        Title:T
        Artist:A
        Creator:C
        Version:Hard

        [Difficulty]
        HPDrainRate:5
        CircleSize:4
        OverallDifficulty:5
        ApproachRate:5
        SliderMultiplier:1.4
        SliderTickRate:1

        [Events]
        0,0,"bg.jpg",0,0
        2,5000,8000
        Sprite,Pass,Centre,"a.png",320,240
         F,0,0,1000,0,1
         L,0,3
          M,0,0,100,1,2,3,4
        Animation,Fail,Centre,"b.png",0,0,2,100,LoopOnce
        Sample,10,0,"s.wav",70

        [TimingPoints]
        0,500,4,2,1,60,1,0
        2000,-50,4,2,1,60,0,1

        [Colours]
        Combo1 : 255,0,0

        [HitObjects]
        100,100,1000,5,2,1:2:3:40:h.wav
        200,100,1500,2,0,B|250:100|300:150,2,100,2|0|4,1:0|0:2|0:0,0:0:0:0:
        256,192,4000,12,0,4800,0:0:0:0:
        64,192,9000,1,0,0:0:0:0:
        """;

    private static readonly List<BeatmapCheck> Checks = typeof(Common)
        .Assembly.GetTypes()
        .Where(type =>
            !type.IsAbstract
            && type.IsSubclassOf(typeof(BeatmapCheck))
            && type.IsDefined(typeof(CheckAttribute), false)
        )
        .Select(type => (BeatmapCheck)Activator.CreateInstance(type)!)
        .ToList();

    private static string Code(int mode) => Map.Replace("\r", "").Replace("MODE", $"{mode}");

    /// <summary> Whether it was read, as opposed to saying that it can't be. </summary>
    private static Beatmap? Read(string code)
    {
        try
        {
            return new Beatmap(code, Path.GetTempPath(), $"{Guid.NewGuid():N}.osu");
        }
        catch (InvalidBeatmapDataException)
        {
            return null;
        }
    }

    private static void RunChecks(Beatmap beatmap)
    {
        foreach (var check in Checks)
        {
            var modes = ((BeatmapCheckMetadata)check.GetMetadata()).Modes;

            if (modes.Contains(beatmap.GeneralSettings.mode))
                Assert.All(check.GetIssues(beatmap), issue => Assert.NotNull(issue.message));
        }
    }

    /// <summary> Numbers are replaced by extreme ones, which nothing may take forever on. </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ExtremeValues_AreRefusedOrChecked(int mode)
    {
        var code = Code(mode);
        var start = code.IndexOf("[TimingPoints]", StringComparison.Ordinal);
        var read = 0;

        var run = Task.Run(() =>
            Parallel.For(
                start,
                code.Length,
                i =>
                {
                    if (!char.IsDigit(code[i]))
                        return;

                    foreach (var value in new[] { "0", "-", "1e7", "1e99", "NaN", "" })
                    {
                        // Whole numbers rather than digits of them.
                        var from = i;
                        var to = i + 1;
                        while (char.IsDigit(code[from - 1]))
                            from--;
                        while (to < code.Length && char.IsDigit(code[to]))
                            to++;

                        if (from != i || Read(code[..from] + value + code[to..]) is not { } beatmap)
                            continue;

                        Interlocked.Increment(ref read);
                        RunChecks(beatmap);
                    }
                }
            )
        );

        Assert.Same(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromMinutes(2))));
        await run;
        Assert.True(read > 50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CutOffAnywhere_IsRefusedOrRead(int mode)
    {
        var code = Code(mode);
        var read = 0;

        for (var length = 0; length < code.Length; length++)
            if (Read(code[..length]) != null)
                read++;

        Assert.True(read > code.Length / 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MissingLines_AreRefusedOrChecked(int mode)
    {
        var lines = Code(mode).Split('\n');

        for (var i = 0; i < lines.Length; i++)
            if (Read(string.Join("\n", lines.Where((_, j) => j != i))) is { } beatmap)
                RunChecks(beatmap);
    }

    [Theory]
    [InlineData("Combo1 : 255,0,0", "Combo1 : 255,0", "[Colours]")]
    [InlineData("CircleSize:4", "CircleSize:big", "[Difficulty]")]
    [InlineData(
        "0,500,4,2,1,60,1,0",
        "0,fast,4,2,1,60,1,0",
        "\"0,fast,4,2,1,60,1,0\" in [TimingPoints]"
    )]
    [InlineData("2,5000,8000", "2,5000", "\"2,5000\" in [Events]")]
    [InlineData("64,192,9000,1,0,0:0:0:0:", "64,192", "\"64,192\" in [HitObjects]")]
    [InlineData("|300:150,2,100", "|300:150,9001,100", "[HitObjects]")]
    [InlineData("|300:150,2,100", "|300:150,2,131073", "[HitObjects]")]
    [InlineData("|300:150,2,100", "|300:131073,2,100", "[HitObjects]")]
    [InlineData("64,192,9000", "64,-131073,9000", "[HitObjects]")]
    [InlineData("64,192,9000", "64,192,2147483648", "[HitObjects]")]
    [InlineData("64,192,9000", "64,192,NaN", "[HitObjects]")]
    [InlineData("12,0,4800", "12,0,NaN", "[HitObjects]")]
    public void Unreadable_SaysWhere(string line, string damaged, string expected)
    {
        var code = Code(0).Replace(line, damaged);

        Assert.Contains(
            expected,
            Assert
                .Throws<InvalidBeatmapDataException>(() =>
                    new Beatmap(code, "song", $"{Guid.NewGuid():N}.osu")
                )
                .Message
        );
    }

    private static List<string> SetFailures(string code)
    {
        var folder = Directory.CreateTempSubdirectory("MapsetVerifierChecksTests").FullName;

        try
        {
            File.WriteAllText(Path.Combine(folder, "a.osu"), code);
            File.WriteAllText(
                Path.Combine(folder, "b.osu"),
                code.Replace("Version:Hard", "Version:Easy")
            );

            BeatmapSet set;

            try
            {
                set = new BeatmapSet(folder);
            }
            catch (InvalidBeatmapDataException)
            {
                return [];
            }

            var failures = new List<string>();

            foreach (var check in SetChecks)
            {
                try
                {
                    _ = check switch
                    {
                        GeneralCheck general => general.GetIssues(set).Count(),
                        BeatmapSetCheck ofSet
                            when set.Beatmaps.Any(beatmap =>
                                ((BeatmapCheckMetadata)check.GetMetadata()).Modes.Contains(
                                    beatmap.GeneralSettings.mode
                                )
                            ) => ofSet.GetIssues(set).Count(),
                        _ => 0,
                    };
                }
                catch (Exception exception)
                {
                    failures.Add($"{check.GetType().Name}: {exception.Message}");
                }
            }

            return failures;
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static readonly List<Check> SetChecks = typeof(Common)
        .Assembly.GetTypes()
        .Where(type =>
            !type.IsAbstract
            && type.IsDefined(typeof(CheckAttribute), false)
            && !type.IsSubclassOf(typeof(BeatmapCheck))
        )
        .Select(type => (Check)Activator.CreateInstance(type)!)
        .ToList();

    [Theory]
    [InlineData("CircleSize:4", "CircleSize:0")]
    [InlineData("CircleSize:4", "CircleSize:-3")]
    [InlineData("CircleSize:4", "CircleSize:1000")]
    [InlineData("0,500,4", "0,0,4")]
    [InlineData("0,500,4", "0,-500,4")]
    [InlineData("0,500,4", "1e99,500,4")]
    [InlineData("0,500,4", "0,1e99,4")]
    [InlineData("0,500,4", "0,1e-99,4")]
    [InlineData("0,500,4", "0,NaN,4")]
    [InlineData("0,500,4", "0,500,0")]
    [InlineData("2000,-50,4", "2000,-1e99,4")]
    [InlineData("2000,-50,4", "2000,NaN,4")]
    [InlineData("64,192,9000", "64,192,2147483647")]
    [InlineData("64,192,9000", "-600,192,9000")]
    [InlineData("2,5000,8000", "2,8000,5000")]
    [InlineData("SliderMultiplier:1.4", "SliderMultiplier:0")]
    [InlineData("SliderTickRate:1", "SliderTickRate:0")]
    public async Task SetsWithExtremes_AreRefusedOrChecked(string line, string damaged)
    {
        for (var mode = 0; mode < 4; mode++)
        {
            var code = Code(mode).Replace(line, damaged);
            var run = Task.Run(() => SetFailures(code));

            Assert.Same(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromMinutes(1))));
            Assert.Empty(await run);
        }
    }

    [Theory]
    [InlineData("|300:150,2,100", "|300:150,2,131072")]
    [InlineData("64,192,9000", "64,-131072,9000")]
    public void AsFarAsTheGameReads_IsRead(string line, string damaged)
    {
        Assert.NotNull(Read(Code(0).Replace(line, damaged)));
    }
}
