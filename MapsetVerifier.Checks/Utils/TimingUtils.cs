using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Shared timing-section helpers used by checks such as
///     <see cref="AllModes.Timing.CheckUnusedLines" />.
/// </summary>
public static class TimingUtils
{
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
}
