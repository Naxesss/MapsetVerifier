using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.HitSounds;

public class CheckManiaStoryHitSoundsTests
{
    private static readonly string[] FreeChord =
    [
        ManiaOsu.Note(1000),
        ManiaOsu.Note(1000, column: ManiaOsu.Column2),
    ];

    /// <summary> The notes are at 70% volume. </summary>
    [Theory]
    [InlineData(70, true)]
    [InlineData(60, true)]
    [InlineData(100, true)]
    [InlineData(59, false)]
    [InlineData(0, false)]
    public void SimilarVolume_IsReported(int volume, bool reported) =>
        Assert.Equal(reported ? 1 : 0, Run([StorySample(1000, volume)], FreeChord).Count);

    [Fact]
    public void Issue_HasTimeAmountAndSample()
    {
        var issue = Assert.Single(
            Run(
                [StorySample(1000), StorySample(1000), StorySample(2000)],
                [
                    ManiaOsu.Note(1000, HitObject.HitSounds.Clap),
                    ManiaOsu.Note(1000, column: ManiaOsu.Column2),
                    ManiaOsu.Note(2000, HitObject.HitSounds.Clap),
                    ManiaOsu.Note(2000, column: ManiaOsu.Column2),
                ]
            )
        );

        // The first chord has a single free note, so only one of its samples can be moved.
        Assert.Contains("00:01:000 - 00:02:000 - ", issue.message);
        Assert.Contains("2 storyboarded hit sounds could be on a note", issue.message);
        Assert.Contains("\"crash.wav\"", issue.message);
    }

    [Fact]
    public void SingleNote_KeepsItsHitNormal() =>
        Assert.Empty(Run([StorySample(1000)], [ManiaOsu.Note(1000)]));

    [Fact]
    public void FileNote_DoesNotPlayTheHitNormal() =>
        Assert.Empty(
            Run(
                [StorySample(1000)],
                [
                    ManiaOsu.Note(1000, fileName: "kick.wav"),
                    ManiaOsu.Note(1000, column: ManiaOsu.Column2),
                    ManiaOsu.Note(2000),
                ]
            )
        );

    [Fact]
    public void NoFreeNote_IsNotReported() =>
        Assert.Empty(
            Run(
                [StorySample(1000)],
                [
                    ManiaOsu.Note(1000, HitObject.HitSounds.Clap),
                    ManiaOsu.Note(1000, HitObject.HitSounds.Finish, ManiaOsu.Column2),
                ]
            )
        );

    [Fact]
    public void NoNoteAtThatTime_IsNotReported() =>
        Assert.Empty(Run([StorySample(1250)], FreeChord));

    [Fact]
    public void FailLayer_IsNotReported() =>
        Assert.Empty(Run([StorySample(1000, layer: 1)], FreeChord));

    [Fact]
    public void Keysounded_IsNotReported() =>
        Assert.Empty(
            Run(
                [StorySample(1000)],
                [
                    .. FreeChord,
                    ManiaOsu.Note(2000, fileName: "a.wav"),
                    ManiaOsu.Note(3000, fileName: "b.wav"),
                ]
            )
        );

    [Fact]
    public void NoteVolume_OverridesTheLine() =>
        Assert.Empty(
            Run(
                [StorySample(1000)],
                [
                    $"{ManiaOsu.Column1},192,1000,1,0,0:0:0:90:",
                    $"{ManiaOsu.Column2},192,1000,1,0,0:0:0:90:",
                ]
            )
        );

    [Fact]
    public void OsbSamples_AreReported()
    {
        using var context = CheckTestContext.CreateFromOsu(
            Map([], FreeChord),
            extraFileContents:
            [
                ("MapsetVerifier - MapsetVerifier (Tests).osb", $"[Events]\n{StorySample(1000)}\n"),
            ]
        );

        Assert.Single(context.RunBeatmapSetCheck<CheckManiaStoryHitSounds>());
    }

    private static string StorySample(int time, int volume = 70, int layer = 0) =>
        $"Sample,{time},{layer},\"crash.wav\",{volume}";

    private static OsuBuilder Map(string[] events, string[] notes) =>
        new OsuBuilder()
            .Mode(Beatmap.Mode.Mania)
            .Events(events)
            .TimingPoints(TestTimingPoints.Uninherited(0, 500, volume: 70))
            .HitObjects(notes);

    private static List<Issue> Run(string[] events, string[] notes)
    {
        using var context = CheckTestContext.CreateFromOsu(Map(events, notes));

        return context.RunBeatmapSetCheck<CheckManiaStoryHitSounds>();
    }
}
