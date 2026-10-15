using System.Globalization;

namespace MapsetVerifier.Checks.Tests;

/// <summary>
/// Builds wiki-shaped <c>[TimingPoints]</c> lines for check tests.
/// Field order: time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
/// </summary>
public static class TestTimingPoints
{
    public static string Uninherited(
        double time,
        double beatLength,
        int meter = 4,
        int sampleSet = 2,
        int sampleIndex = 0,
        int volume = 100,
        int effects = 0
    ) => Format(time, beatLength, meter, sampleSet, sampleIndex, volume, uninherited: 1, effects);

    public static string Inherited(
        double time,
        double beatLength,
        int meter = 4,
        int sampleSet = 2,
        int sampleIndex = 0,
        int volume = 100,
        int effects = 0
    ) => Format(time, beatLength, meter, sampleSet, sampleIndex, volume, uninherited: 0, effects);

    private static string Format(
        double time,
        double beatLength,
        int meter,
        int sampleSet,
        int sampleIndex,
        int volume,
        int uninherited,
        int effects
    )
    {
        var inv = CultureInfo.InvariantCulture;
        return string.Join(
            ",",
            time.ToString(inv),
            beatLength.ToString(inv),
            meter.ToString(inv),
            sampleSet.ToString(inv),
            sampleIndex.ToString(inv),
            volume.ToString(inv),
            uninherited.ToString(inv),
            effects.ToString(inv)
        );
    }
}
