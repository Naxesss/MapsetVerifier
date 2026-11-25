using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

/// <summary> Every check has to cope with maps that are unfinished or out of the ordinary. </summary>
public class UnusualMapTests
{
    private const string Red = "0,500,4,2,0,100,1,0";

    private static readonly List<Check> Checks = typeof(Common)
        .Assembly.GetTypes()
        .Where(type =>
            !type.IsAbstract
            && type.IsSubclassOf(typeof(Check))
            && type.IsDefined(typeof(CheckAttribute), false)
        )
        .Select(type => (Check)Activator.CreateInstance(type)!)
        .ToList();

    private static readonly Dictionary<
        string,
        (string[] Objects, string[] Timing, string[] Events)
    > Maps = new()
    {
        ["no objects"] = ([], [Red], []),
        ["no objects or timing"] = ([], [], []),
        ["only green lines"] = (
            ["100,100,1000,1,0,0:0:0:0:", "300,100,1500,1,4,0:0:0:0:", "300,100,2000,1,8,0:0:0:0:"],
            ["0,-100,4,2,0,100,0,0", "1200,-50,4,2,0,60,0,1"],
            []
        ),
        ["only green lines and spinners"] = (
            [
                "100,100,1000,5,0,0:0:0:0:",
                "256,192,1200,12,0,2000,0:0:0:0:",
                "300,100,2100,1,0,0:0:0:0:",
                "300,100,2200,1,8,0:0:0:0:",
                "100,100,2300,1,0,0:0:0:0:",
                "100,100,2400,1,8,0:0:0:0:",
                "100,100,2500,1,0,0:0:0:0:",
                "256,192,2600,12,0,4000,0:0:0:0:",
            ],
            [
                "0,-100,4,2,0,100,0,0",
                "1000,-100,4,2,0,100,0,1",
                "1100,-50,4,2,0,60,0,0",
                "1300,-50,4,2,0,60,0,1",
            ],
            []
        ),
        ["one object"] = (["100,100,1000,1,0,0:0:0:0:"], [Red], []),
        ["one slider"] = (["100,100,1000,2,0,L|240:100,1,140"], [Red], []),
        ["spinners"] = (
            ["256,192,1000,12,0,2000,0:0:0:0:", "256,192,3000,12,0,9000,0:0:0:0:"],
            [Red],
            []
        ),
        ["everything before the first line"] = (
            [
                "100,100,1000,1,0,0:0:0:0:",
                "256,192,2000,12,0,3000,0:0:0:0:",
                "100,100,4000,2,0,L|240:100,2,140",
            ],
            ["20000,500,4,2,0,100,1,0"],
            []
        ),
        ["on top of each other"] = (
            [
                "100,100,1000,1,0,0:0:0:0:",
                "100,100,1000,1,0,0:0:0:0:",
                "100,100,1000,2,0,L|240:100,2,140",
                "256,192,1000,12,0,1200,0:0:0:0:",
                "100,100,1100,1,0,0:0:0:0:",
            ],
            [Red, Red, "0,-100,4,2,0,100,0,0"],
            []
        ),
        ["kiai and breaks around nothing"] = (
            ["100,100,30000,1,0,0:0:0:0:", "100,100,60000,1,0,0:0:0:0:"],
            [Red, "1000,-100,4,2,0,100,0,1", "2000,-100,4,2,0,100,0,0", "90000,-50,4,2,0,5,0,1"],
            ["2,1000,20000", "2,40000,50000", "2,70000,80000"]
        ),
        ["sliders without length"] = (
            [
                "100,100,1000,2,0,B|100:100,1,0",
                "100,100,2000,2,0,L|100:100,1,1",
                "100,100,3000,2,0,P|100:100|100:100,2,10",
            ],
            [Red],
            []
        ),
        ["missing files"] = (
            ["100,100,1000,1,0,0:0:0:0:missing.wav"],
            [Red],
            [
                "0,0,\"missing.jpg\",0,0",
                "0,0,\"\",0,0",
                "Video,0,\"missing.mp4\"",
                "Sprite,Pass,Centre,\"missing.png\",320,240",
                "Animation,Pass,Centre,\"missing.png\",320,240,2,100,LoopForever",
                "Sample,0,0,\"missing.wav\"",
            ]
        ),
        ["slow and fast"] = (
            [
                "100,100,1000,1,0,0:0:0:0:",
                "400,100,6001,1,0,0:0:0:0:",
                "100,100,20000,1,0,0:0:0:0:",
                "400,100,20010,1,0,0:0:0:0:",
                "100,100,20020,1,0,0:0:0:0:",
            ],
            ["0,6000,4,2,0,100,1,0", "20000,10,1,2,0,100,1,8", "20015,-1000,4,2,0,100,0,0"],
            []
        ),
    };

    public static TheoryData<string, Beatmap.Mode> EveryMapAndMode()
    {
        var data = new TheoryData<string, Beatmap.Mode>();

        foreach (var map in Maps.Keys)
        foreach (var mode in Enum.GetValues<Beatmap.Mode>())
            data.Add(map, mode);

        return data;
    }

    private static string Build(string map, Beatmap.Mode mode, string version) =>
        new OsuBuilder()
            .Mode(mode)
            .Version(version)
            .TimingPoints(Maps[map].Timing)
            .HitObjects(Maps[map].Objects)
            .Events(Maps[map].Events)
            .Build();

    private static List<string> Failures(BeatmapSet set)
    {
        var failures = new List<string>();

        foreach (var check in Checks)
        {
            try
            {
                switch (check)
                {
                    case GeneralCheck general:
                        Assert.All(general.GetIssues(set), issue => Assert.NotNull(issue.message));
                        break;
                    // As the app does, skips checks for modes the set doesn't have.
                    case BeatmapSetCheck ofSet:
                        var setModes = ((BeatmapCheckMetadata)check.GetMetadata()).Modes;

                        if (
                            !set.Beatmaps.Any(beatmap =>
                                setModes.Contains(beatmap.GeneralSettings.mode)
                            )
                        )
                            break;

                        Assert.All(ofSet.GetIssues(set), issue => Assert.NotNull(issue.message));
                        break;
                    case BeatmapCheck ofMap:
                        var modes = ((BeatmapCheckMetadata)check.GetMetadata()).Modes;

                        foreach (
                            var beatmap in set.Beatmaps.Where(beatmap =>
                                modes.Contains(beatmap.GeneralSettings.mode)
                            )
                        )
                            Assert.All(
                                ofMap.GetIssues(beatmap),
                                issue => Assert.NotNull(issue.message)
                            );
                        break;
                }
            }
            catch (Exception exception)
            {
                failures.Add(
                    $"{check.GetType().Name}: {exception.GetType().Name} {exception.Message} "
                        + exception.StackTrace?.Split('\n')[0].Trim()
                );
            }
        }

        return failures;
    }

    [Theory]
    [MemberData(nameof(EveryMapAndMode))]
    public void EveryCheck_CopesWith(string map, Beatmap.Mode mode)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("a.osu", Build(map, mode, "Easy")),
            ("b.osu", Build(map, mode, "Insane")),
        ]);

        Assert.Empty(Failures(context.BeatmapSet));
    }

    [Theory]
    [InlineData("no objects")]
    [InlineData("one slider")]
    [InlineData("missing files")]
    public void EveryCheck_CopesWithAllModesInOneSet(string map)
    {
        using var context = CheckTestContext.CreateFromOsuFiles(
            Enum.GetValues<Beatmap.Mode>()
                .Select(mode => ($"{mode}.osu", Build(map, mode, mode.ToString())))
        );

        Assert.Empty(Failures(context.BeatmapSet));
    }

    [Fact]
    public void EveryCheck_CopesWithASetWithoutDifficulties()
    {
        using var context = CheckTestContext.CreateFromOsuFiles([], ["audio.mp3"]);

        Assert.Empty(Failures(context.BeatmapSet));
    }
}
