using MapsetVerifier.Checks.Mania.HitSounds;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Mania.HitSounds;

public class CheckDoubleHitSoundsTests
{
    [Fact]
    public void SameAdditionTwiceInAChord_IsReported()
    {
        var issue = Assert.Single(
            Run(
                ManiaOsu.Note(1000, HitObject.HitSounds.Clap),
                ManiaOsu.Note(1000, HitObject.HitSounds.Clap, ManiaOsu.Column2)
            )
        );

        Assert.Contains("normal-hitclap", issue.message);
        Assert.Contains("2 times at once", issue.message);
    }

    /// <summary>
    ///     The previous implementation cleared its seen samples on every note of a chord instead of between
    ///     chords, so it reported the same addition used at two unrelated times as a double.
    /// </summary>
    [Fact]
    public void SameAdditionAtDifferentTimes_IsNotReported()
    {
        Assert.Empty(
            Run(
                ManiaOsu.Note(1000, HitObject.HitSounds.Clap),
                ManiaOsu.Note(1500, HitObject.HitSounds.Clap),
                ManiaOsu.Note(2000, HitObject.HitSounds.Clap)
            )
        );
    }

    /// <summary> Every note plays a hit normal, so a plain chord isn't a doubled sample. </summary>
    [Fact]
    public void ChordWithoutAdditions_IsNotReported()
    {
        Assert.Empty(
            Run(
                ManiaOsu.Note(1000),
                ManiaOsu.Note(1000, column: ManiaOsu.Column2),
                ManiaOsu.Note(1000, column: ManiaOsu.Column3)
            )
        );
    }

    [Fact]
    public void DifferentAdditionsInAChord_AreNotReported()
    {
        Assert.Empty(
            Run(
                ManiaOsu.Note(1000, HitObject.HitSounds.Clap),
                ManiaOsu.Note(1000, HitObject.HitSounds.Whistle, ManiaOsu.Column2)
            )
        );
    }

    [Fact]
    public void SameReferencedFileTwiceInAChord_IsReported()
    {
        var issue = Assert.Single(
            Run(
                ManiaOsu.Note(1000, fileName: "bell.wav"),
                ManiaOsu.Note(1000, column: ManiaOsu.Column2, fileName: "bell.wav")
            )
        );

        Assert.Contains("bell.wav", issue.message);
    }

    private static List<Issue> Run(params string[] hitObjects)
    {
        using var context = CheckTestContext.CreateFromOsuFiles([
            ("mania.osu", ManiaOsu.Build(hitObjects: hitObjects)),
        ]);

        return context.RunBeatmapCheck<CheckDoubleHitSounds>("Insane");
    }
}
