using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MathNet.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace MapsetVerifier.Parser.Tests.Objects;

public class SliderBodySampleDebugTests(ITestOutputHelper output)
{
    [Fact]
    public void FallenKingdom_163180_SliderBodySamples()
    {
        var mapPath =
            @"d:\Projects\MapsetVerifier\TryHardNinja - Fallen Kingdom (Nyanaro) [Insane].osu";
        var code = File.ReadAllText(mapPath);
        var beatmap = new Beatmap(code, mapPath, mapPath);

        var slider = beatmap.HitObjects.OfType<Slider>().Single(h => h.time == 163180);

        output.WriteLine($"hitSound={slider.hitSound}");
        output.WriteLine($"StartHitSound={slider.StartHitSound}");
        output.WriteLine($"EndTime={slider.EndTime}");
        output.WriteLine($"HasWhistle={slider.HasHitSound(HitObject.HitSounds.Whistle)}");
        output.WriteLine($"sampleset={slider.sampleset} addition={slider.addition}");

        var bodySamples = slider
            .usedHitSamples.Where(sample => sample.HitSource == HitSample.HitSourceType.Body)
            .OrderBy(sample => sample.Time)
            .ToList();

        foreach (var sample in slider.usedHitSamples.OrderBy(s => s.Time))
        {
            output.WriteLine(
                $"{sample.Time:F0} {sample.HitSource} {sample.Sampleset} {sample.HitSound} idx={sample.CustomIndex} file={sample.GetFileName()}"
            );
        }

        Assert.All(bodySamples, sample => Assert.InRange(sample.Time, slider.time - 1, slider.EndTime + 1));
        Assert.Contains(
            bodySamples,
            sample =>
                sample.Time.AlmostEqual(slider.time)
                && sample.HitSound == HitObject.HitSounds.None
        );
        Assert.Contains(
            bodySamples,
            sample =>
                sample.Time.AlmostEqual(slider.time)
                && sample.HitSound == HitObject.HitSounds.Whistle
        );
        Assert.DoesNotContain(bodySamples, sample => sample.Time < slider.time - 1);
    }
}
