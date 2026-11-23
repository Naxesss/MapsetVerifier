using MapsetVerifier.Checks.Standard.HitSounds;
using MapsetVerifier.Framework.Objects;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Standard.HitSounds;

public class CheckLoudSliderTicksTests
{
    [Fact]
    public void LoudTick_IsReported()
    {
        using var context = Create(tickLevel: 0.5, hitLevel: 0.5);

        var issue = Assert.Single(Run(context));
        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.Contains("00:01:500 -", issue.message);
        Assert.Contains("\"soft-slidertick2.wav\" is up to 100%", issue.message);
        Assert.Contains("used 1 time(s)", issue.message);
    }

    [Fact]
    public void QuietTick_IsNotReported()
    {
        using var context = Create(tickLevel: 0.2, hitLevel: 0.5);

        Assert.Empty(Run(context));
    }

    [Fact]
    public void LoudTickAtLowVolume_IsNotReported()
    {
        using var context = Create(
            tickLevel: 0.5,
            hitLevel: 0.5,
            lines: [TestTimingPoints.Inherited(1400, -100, sampleIndex: 2, volume: 30)]
        );

        Assert.Empty(Run(context));
    }

    [Fact]
    public void SilentTick_IsNotReported()
    {
        using var context = Create(tickLevel: 0, hitLevel: 0.5);

        Assert.Empty(Run(context));
    }

    [Fact]
    public void TickUsingDefaultSounds_IsNotReported()
    {
        using var context = Create(tickLevel: 0.5, hitLevel: 0.5, sampleIndex: 0);

        Assert.Empty(Run(context));
    }

    [Fact]
    public void DefaultHitsounds_AreNotCompared()
    {
        using var context = Create(tickLevel: 0.5, hitLevel: null);

        Assert.Empty(Run(context));
    }

    [Fact]
    public void ManyTicks_AreOneIssue()
    {
        using var context = Create(
            tickLevel: 0.5,
            hitLevel: 0.5,
            sliders: Enumerable.Range(0, 8).Select(i => Slider(1000 + i * 2000))
        );

        var issue = Assert.Single(Run(context));
        Assert.Contains("00:09:500 - ...", issue.message);
        Assert.Contains("used 8 time(s)", issue.message);
    }

    [Fact]
    public void UnknownLevel_IsNotReported()
    {
        using var context = Create(tickLevel: 0.5, hitLevel: 0.5, version: "Test");

        Assert.Empty(context.RunBeatmapCheck<CheckLoudSliderTicks>("Test"));
    }

    [Fact]
    public void ShortSounds_AreQuieter()
    {
        var longSound = Enumerable.Repeat(new[] { 0.5f, -0.5f }, 100).ToList();
        var shortSound = Enumerable.Repeat(new[] { 0.5f }, 5).ToList();

        Assert.Equal(0.5, CheckLoudSliderTicks.GetLoudness(longSound), 3);
        Assert.True(CheckLoudSliderTicks.GetLoudness(shortSound) < 0.25);
        Assert.Equal(0, CheckLoudSliderTicks.GetLoudness([]));
    }

    private static List<Issue> Run(CheckTestContext context) =>
        context.RunBeatmapCheck<CheckLoudSliderTicks>("Hard");

    /// <summary> Two beats of 500 ms long, so with a tick halfway. </summary>
    private static string Slider(int time) =>
        TestHitObjects.Slider(time, curvePoints: "256:380", length: 280, y: 100);

    private static CheckTestContext Create(
        double tickLevel,
        double? hitLevel,
        int sampleIndex = 2,
        IEnumerable<string>? lines = null,
        IEnumerable<string>? sliders = null,
        string version = "Hard"
    )
    {
        var osu = new OsuBuilder()
            .Version(version)
            .TimingPoints(TestTimingPoints.Uninherited(0, 500, sampleIndex: sampleIndex))
            .TimingPoints(lines ?? [])
            .HitObjects(sliders ?? [Slider(1000)]);

        List<(string, byte[])> files = [("soft-slidertick2.wav", Wav(tickLevel))];

        if (hitLevel is { } level)
            files.Add(("soft-hitnormal2.wav", Wav(level)));

        return CheckTestContext.CreateFromOsuFiles(
            [("test.osu", osu.Build())],
            extraBinaryFiles: files
        );
    }

    /// <summary> 100 ms of a 1 kHz tone, with the given level from 0 to 1. </summary>
    private static byte[] Wav(double level)
    {
        const int sampleRate = 44100;
        const int dataSize = sampleRate / 10 * 2;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataSize);

        for (var i = 0; i < dataSize / 2; i++)
            writer.Write(
                (short)(Math.Sin(2 * Math.PI * 1000 * i / sampleRate) * level * short.MaxValue)
            );

        writer.Flush();

        return stream.ToArray();
    }
}
