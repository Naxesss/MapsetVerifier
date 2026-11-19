using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Measures the rhythm of a difficulty the way the ranking criteria describe it: in beats of a
///     180 BPM song. A 1/2 gap at 120 BPM is 250 ms, which is 3/4 of a beat at 180 BPM, so the same
///     gap counts as less dense at lower tempos and denser at higher ones, as described on the
///     Scaling BPM page.
/// </summary>
public static class RhythmUtils
{
    /// <summary> Length of a 1/1 beat at the 180 BPM the ranking criteria are written for. </summary>
    public const double ReferenceBeatMs = 60000d / 180;

    /// <summary> Gaps longer than this are pauses, not rhythm, and are left out. </summary>
    public const double MaxRhythmGapBeats = 4;

    /// <summary> 180 / 240 BPM, just under so gaps exactly on a 240 BPM snap don't count as faster. </summary>
    private const double FasterSnapRatio = 0.74;

    /// <summary>
    ///     A gap between two consecutive rhythm events, in milliseconds and in 180 BPM beats.
    ///     <see cref="Time" /> is when the gap ends, i.e. the object that is played after it.
    /// </summary>
    public readonly record struct RhythmGap(double Time, double Ms)
    {
        public double ReferenceBeats => Ms / ReferenceBeatMs;

        /// <summary>
        ///     Whether this gap is faster than the given divisor, such as 1/2 for 2. The Scaling BPM page
        ///     treats 120 and 240 BPM as the points where a snap counts as the next one (1/2 at 240 BPM is
        ///     as dense as 1/4 at 180), so a gap only counts as faster once it is shorter than the snap
        ///     would be at 240 BPM, three quarters of its length at 180 BPM.
        /// </summary>
        public bool IsFasterThan(double divisor) => ReferenceBeats < 1 / divisor * FasterSnapRatio;
    }

    /// <summary>
    ///     Returns the gaps between consecutive rhythm events of a difficulty. What counts as an event
    ///     depends on the mode: object starts in osu!, don and kat notes in osu!taiko, and fruits plus
    ///     slider heads, repeats and tails in osu!catch. A spinner or drumroll in between, or a pause
    ///     of more than <see cref="MaxRhythmGapBeats" /> beats, splits the rhythm instead of being a gap.
    /// </summary>
    public static List<RhythmGap> GetGaps(Beatmap beatmap)
    {
        var gaps = new List<RhythmGap>();
        double? previous = null;

        foreach (var (time, isRhythm) in GetEvents(beatmap))
        {
            if (!isRhythm)
            {
                previous = null;
                continue;
            }

            if (previous is { } previousTime)
            {
                var ms = time - previousTime;

                if (ms > 0 && ms / ReferenceBeatMs <= MaxRhythmGapBeats)
                    gaps.Add(new RhythmGap(time, ms));
            }

            previous = time;
        }

        return gaps;
    }

    /// <summary> Share of the gaps that are faster than the given divisor at 180 BPM, from 0 to 1. </summary>
    public static double GetShareFasterThan(IReadOnlyCollection<RhythmGap> gaps, double divisor) =>
        gaps.Count == 0 ? 0 : gaps.Count(gap => gap.IsFasterThan(divisor)) / (double)gaps.Count;

    /// <summary> Every event in time order; events that are not rhythm split it. </summary>
    private static IEnumerable<(double Time, bool IsRhythm)> GetEvents(Beatmap beatmap)
    {
        switch (beatmap.GeneralSettings.mode)
        {
            case Beatmap.Mode.Taiko:
                foreach (var hitObject in beatmap.HitObjects)
                    yield return (hitObject.time, hitObject is Circle);

                break;

            case Beatmap.Mode.Catch:
                foreach (
                    var catchObject in beatmap
                        .GetCatchHitObjects(includeJuiceStreamParts: true)
                        .Where(catchObject =>
                            catchObject
                                is not JuiceStream.JuiceStreamPart
                                {
                                    Kind: JuiceStream.JuiceStreamPart.PartKind.Drop
                                }
                        )
                        .OrderBy(catchObject => catchObject.Time)
                )
                    yield return (catchObject.Time, catchObject is not Bananas);

                break;

            default:
                foreach (var hitObject in beatmap.HitObjects)
                {
                    if (hitObject is Spinner)
                    {
                        yield return (hitObject.time, false);
                        continue;
                    }

                    yield return (hitObject.time, true);
                }

                break;
        }
    }
}
