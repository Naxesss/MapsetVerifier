using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Finds the rhythm and structure patterns the osu! difficulty guidelines talk about, such as runs of
///     clicked 1/2 or streams. Gaps are measured in 180 BPM beats, see <see cref="RhythmUtils" />.
///     A "1/2" here is a gap that counts as 1/2 at 180 BPM: faster than 1/1, but not faster than 1/2.
/// </summary>
public static class OsuPatterns
{
    /// <summary> A run of objects forming a pattern, with how many of what the pattern counts. </summary>
    public readonly record struct Run(HitObject First, HitObject Last, int Count)
    {
        public double ReferenceBeats =>
            (Last.GetEndTime() - First.time) / RhythmUtils.ReferenceBeatMs;
    }

    /// <summary> The longest run of each pattern in a difficulty, as measured by the report script. </summary>
    public readonly record struct Measurements(
        int ClickedHalfRun,
        int HalfSliderRun,
        int HalfGapChain,
        double LongestWithoutRest,
        int LongestStream,
        int LongestCombo,
        double SvChangesPerMinute
    );

    public static Measurements Measure(Beatmap beatmap) =>
        new(
            MaxCount(ClickedHalfRuns(beatmap)),
            MaxCount(HalfSliderRuns(beatmap)),
            MaxCount(HalfGapChains(beatmap)),
            SectionsWithoutRest(beatmap).Select(run => run.ReferenceBeats).DefaultIfEmpty(0).Max(),
            MaxCount(Streams(beatmap)),
            MaxCount(Combos(beatmap)),
            SvChangesPerMinute(beatmap)
        );

    /// <summary> Whether a gap counts as 1/2 at 180 BPM. </summary>
    public static bool IsHalf(double ms)
    {
        var gap = new RhythmUtils.RhythmGap(0, ms);
        return gap.IsFasterThan(1) && !gap.IsFasterThan(2);
    }

    /// <summary> Whether a gap counts as 1/1 or longer at 180 BPM, a moment to rest. </summary>
    public static bool IsRest(double ms) => !new RhythmUtils.RhythmGap(0, ms).IsFasterThan(1);

    /// <summary> Runs of clicks 1/2 apart, start to start. Counts the 1/2 rhythms, so three clicks are 2. </summary>
    public static IEnumerable<Run> ClickedHalfRuns(Beatmap beatmap) =>
        Runs(beatmap, (previous, next) => IsHalf(next.time - previous.time), countObjects: false);

    /// <summary> Runs of objects with 1/2 gaps from the end of one to the start of the next. Counts the gaps. </summary>
    public static IEnumerable<Run> HalfGapChains(Beatmap beatmap) =>
        Runs(
            beatmap,
            (previous, next) => IsHalf(next.time - previous.GetEndTime()),
            countObjects: false
        );

    /// <summary> Runs of objects whose starts are faster than 1/2 apart. Counts the notes. </summary>
    public static IEnumerable<Run> Streams(Beatmap beatmap) =>
        Runs(
            beatmap,
            (previous, next) =>
                new RhythmUtils.RhythmGap(0, next.time - previous.time).IsFasterThan(2),
            countObjects: true
        );

    /// <summary>
    ///     Runs of 1/2 sliders, each followed by the next with a 1/2 gap, so the rhythm never stops. Counts
    ///     the sliders.
    /// </summary>
    public static IEnumerable<Run> HalfSliderRuns(Beatmap beatmap) =>
        Runs(
            beatmap,
            (previous, next) =>
                IsHalfSlider(previous)
                && IsHalfSlider(next)
                && IsHalf(next.time - previous.GetEndTime()),
            countObjects: true
        );

    /// <summary>
    ///     Stretches of gameplay without a 1/1 or longer gap between the end of one object and the start of
    ///     the next. Spinners count as rest. Counts the objects; see <see cref="Run.ReferenceBeats" /> for length.
    /// </summary>
    public static IEnumerable<Run> SectionsWithoutRest(Beatmap beatmap) =>
        Runs(
            beatmap,
            (previous, next) => !IsRest(next.time - previous.GetEndTime()),
            countObjects: true
        );

    /// <summary> Every combo with its number of objects. Spinners start and end combos, and aren't counted. </summary>
    public static IEnumerable<Run> Combos(Beatmap beatmap)
    {
        HitObject? first = null;
        HitObject? last = null;
        var count = 0;
        var afterSpinner = false;

        foreach (var hitObject in beatmap.HitObjects)
        {
            if (hitObject is Spinner)
            {
                if (first != null)
                    yield return new Run(first, last!, count);

                first = null;
                afterSpinner = true;
                continue;
            }

            if (first != null && (hitObject.HasType(HitObject.Types.NewCombo) || afterSpinner))
            {
                yield return new Run(first, last!, count);
                first = null;
            }

            first ??= hitObject;
            last = hitObject;
            count = first == hitObject ? 1 : count + 1;
            afterSpinner = false;
        }

        if (first != null)
            yield return new Run(first, last!, count);
    }

    /// <summary>
    ///     How often the slider velocity changes between one slider and the next, per minute of gameplay.
    ///     Only changes a player can see count, so green lines that don't change the speed of any slider
    ///     are left out.
    /// </summary>
    public static double SvChangesPerMinute(Beatmap beatmap)
    {
        var objects = beatmap.HitObjects;

        if (objects.Count < 2)
            return 0;

        var minutes = (objects[^1].GetEndTime() - objects[0].time) / 60000;

        if (minutes <= 0)
            return 0;

        double? previous = null;
        var changes = 0;

        foreach (var slider in objects.OfType<Slider>())
        {
            var velocity = Math.Round(beatmap.GetSliderVelocityLine(slider.time)?.SvMult ?? 1, 2);

            if (previous is { } previousVelocity && Math.Abs(previousVelocity - velocity) > 0.001)
                changes++;

            previous = velocity;
        }

        return Math.Round(changes / minutes, 1);
    }

    private static bool IsHalfSlider(HitObject hitObject) =>
        hitObject is Slider slider && IsHalf(slider.GetEndTime() - slider.time);

    private static int MaxCount(IEnumerable<Run> runs) =>
        runs.Select(run => run.Count).DefaultIfEmpty(0).Max();

    /// <summary>
    ///     Groups consecutive objects into runs while <paramref name="continues" /> holds between each pair.
    ///     Spinners always split runs. Only runs of at least two objects are returned.
    /// </summary>
    private static IEnumerable<Run> Runs(
        Beatmap beatmap,
        Func<HitObject, HitObject, bool> continues,
        bool countObjects
    )
    {
        HitObject? first = null;
        HitObject? previous = null;
        var links = 0;

        foreach (var hitObject in beatmap.HitObjects)
        {
            if (hitObject is Spinner)
            {
                if (links > 0)
                    yield return new Run(first!, previous!, countObjects ? links + 1 : links);

                first = previous = null;
                links = 0;
                continue;
            }

            if (previous != null && continues(previous, hitObject))
            {
                links++;
            }
            else
            {
                if (links > 0)
                    yield return new Run(first!, previous!, countObjects ? links + 1 : links);

                first = hitObject;
                links = 0;
            }

            first ??= hitObject;
            previous = hitObject;
        }

        if (links > 0)
            yield return new Run(first!, previous!, countObjects ? links + 1 : links);
    }
}
