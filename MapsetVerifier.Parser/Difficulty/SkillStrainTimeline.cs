using System.Reflection;
using MapsetVerifier.Parser.Objects;
using osu.Game.Rulesets.Difficulty.Skills;

namespace MapsetVerifier.Parser.Difficulty;

/// <summary>
///     Turns a processed <see cref="Skill" /> into a chronological difficulty-over-time timeline.
///     <para>
///         Mirrors how osu! itself charts strain (<c>osu-tools</c>'
///         <a href="https://github.com/ppy/osu-tools/blob/master/PerformanceCalculatorGUI/Screens/Simulate/StrainVisualizer.cs">
///             <c>StrainVisualizer</c>
///         </a>
///         ), so the overview charts line up with what PerformanceCalculatorGUI/lazer show:
///         <see cref="StrainSkill" />s are charted from their section strain peaks, and every other
///         skill type from its per-object difficulties anchored to the objects' own times.
///     </para>
/// </summary>
public static class SkillStrainTimeline
{
    /// <summary>
    ///     <see cref="StrainSkill.SectionLength" /> is protected, and rulesets override it
    ///     (osu!catch's Movement uses 750ms). Falls back to osu!'s own default.
    /// </summary>
    private const int DefaultSectionLength = 400;

    private static readonly PropertyInfo? SectionLengthProperty = typeof(StrainSkill).GetProperty(
        "SectionLength",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    public static List<StrainInterval> Build(Skill skill, Beatmap beatmap) =>
        skill switch
        {
            // Note the deliberate absence of a VariableLengthStrainSkill case (osu!std's Aim).
            // Its GetCurrentStrainPeaks() is not a timeline: peaks are inserted sorted by value
            // (highest first) and the lowest ones are discarded once the map exceeds ~44s, since
            // the class only ever intends them to be fed into a weighted sum. Reading it as a
            // timeline produces a graph that starts at the map's peak, decays monotonically, and
            // falls off a cliff around 0:45. It goes through the per-object path instead, which is
            // also what osu-tools does with it.
            StrainSkill strainSkill => BuildFromStrainPeaks(strainSkill, beatmap),
            _ => BuildFromObjectDifficulties(skill, beatmap),
        };

    /// <summary>
    ///     <see cref="StrainSkill" /> divides the map into fixed <c>SectionLength</c> sections and keeps
    ///     the highest strain in each. The first section ends at the first processed object's time
    ///     rounded up to a section boundary (see <c>StrainSkill.ProcessInternal</c>), and each
    ///     subsequent peak covers one section after that.
    /// </summary>
    private static List<StrainInterval> BuildFromStrainPeaks(StrainSkill skill, Beatmap beatmap)
    {
        var peaks = skill.GetCurrentStrainPeaks().ToList();
        if (peaks.Count == 0)
            return [];

        var sectionLength = GetSectionLength(skill);
        var firstSectionEnd =
            Math.Ceiling(GetFirstProcessedObjectTime(beatmap) / sectionLength) * sectionLength;

        var intervals = new List<StrainInterval>(peaks.Count);

        for (var index = 0; index < peaks.Count; index++)
        {
            var end = firstSectionEnd + index * sectionLength;
            intervals.Add(new StrainInterval(end - sectionLength, end, peaks[index]));
        }

        return intervals;
    }

    /// <summary>
    ///     Per-object difficulties are index-aligned with the objects the ruleset processed, so each
    ///     one covers its own object. A value is held for up to one section length past the object's
    ///     end (matching how strain sections linger), then drops to zero until the next object - which
    ///     is what makes breaks read as zero difficulty instead of holding the last note's value.
    /// </summary>
    private static List<StrainInterval> BuildFromObjectDifficulties(Skill skill, Beatmap beatmap)
    {
        var difficulties = skill.GetObjectDifficulties();
        var timings = beatmap.DifficultyObjectTimings;

        var count = Math.Min(difficulties.Count, timings.Count);
        if (count == 0)
            return [];

        var intervals = new List<StrainInterval>(count);

        for (var index = 0; index < count; index++)
        {
            var timing = timings[index];
            var start = timing.StartTime;
            var end = timing.EndTime + DefaultSectionLength;

            // Objects can overlap (2B, catch juice streams); never let one interval run into the next.
            var nextStart = index + 1 < count ? timings[index + 1].StartTime : (double?)null;
            if (nextStart.HasValue)
                end = Math.Min(end, nextStart.Value);

            end = Math.Max(end, start);
            intervals.Add(new StrainInterval(start, end, difficulties[index]));

            if (nextStart.HasValue && nextStart.Value > end)
                intervals.Add(new StrainInterval(end, nextStart.Value, 0));
        }

        return intervals;
    }

    private static double GetFirstProcessedObjectTime(Beatmap beatmap)
    {
        if (beatmap.DifficultyObjectTimings.Count > 0)
            return beatmap.DifficultyObjectTimings[0].StartTime;

        return beatmap.HitObjects.Count > 0
            ? beatmap.HitObjects.Min(hitObject => hitObject.time)
            : 0;
    }

    private static int GetSectionLength(StrainSkill skill) =>
        SectionLengthProperty?.GetValue(skill) as int? ?? DefaultSectionLength;
}
