using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Shared timing-section helpers used by checks such as
///     <see cref="AllModes.Timing.CheckUnusedLines" />.
/// </summary>
public static class TimingUtils
{
    /// <summary>
    ///     Returns the standard snap divisor whose beat length (<paramref name="msPerBeat" /> / divisor)
    ///     is closest to <paramref name="durationMs" />.
    /// </summary>
    public static int GetClosestSnapDivisor(double durationMs, double msPerBeat) =>
        Beatmap.SnapDivisors.MinBy(d => Math.Abs(durationMs - msPerBeat / d));

    /// <summary>
    ///     Returns whether this timing section contains the respective hit object type.
    ///     Only counts the start of objects.
    /// </summary>
    public static bool SectionContainsObject<T>(Beatmap beatmap, TimingLine line)
        where T : HitObject
    {
        var nextLine = line.Next(true);

        if (nextLine == null)
            return beatmap.GetNextHitObject<T>(line.Offset) != null;

        var nextSectionEnd = nextLine.Offset;
        var objectTimeBeforeEnd = beatmap.GetPrevHitObject<T>(nextSectionEnd)?.time ?? 0;

        return objectTimeBeforeEnd >= line.Offset;
    }

    /// <summary>
    ///     Returns whether this timing section overlaps a hit object of the respective type at any point
    ///     during its duration, not just its start. Unlike <see cref="SectionContainsObject{T}" />, this
    ///     also counts objects which started before the section but still run into it, e.g. a slider whose
    ///     ticks, repeats, or tail (each of which use the timing line active at their own time, not the
    ///     line at the slider's start) fall within this section.
    /// </summary>
    public static bool SectionOverlapsObject<T>(Beatmap beatmap, TimingLine line)
        where T : HitObject
    {
        var sectionStart = line.Offset;
        var nextLine = line.Next(true);
        var sectionEnd = nextLine?.Offset ?? double.MaxValue;

        return beatmap
            .HitObjects.OfType<T>()
            .Any(hitObject =>
                hitObject.time < sectionEnd && hitObject.GetEndTime() >= sectionStart
            );
    }

    /// <summary>
    ///     Returns whether two uninherited lines share the same BPM, meter, and downbeat alignment.
    /// </summary>
    public static bool AreDownbeatsAligned(UninheritedLine line, UninheritedLine otherLine)
    {
        var negligibleDownbeatOffset = GetBeatOffset(otherLine, line, otherLine.Meter) <= 1;

        return otherLine.bpm.AlmostEqual(line.bpm)
            && otherLine.Meter == line.Meter
            && negligibleDownbeatOffset;
    }

    private static double GetBeatOffset(
        UninheritedLine line,
        UninheritedLine nextLine,
        double beatModulo
    )
    {
        var beatsIn = (nextLine.Offset - line.Offset) / line.msPerBeat;
        var offset = beatsIn % beatModulo;

        return Math.Min(Math.Abs(offset), Math.Abs(offset - beatModulo)) * line.msPerBeat;
    }
}
