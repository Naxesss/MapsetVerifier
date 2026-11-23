using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.HitObjects.Taiko;
using MapsetVerifier.Parser.Objects.TimingLines;

namespace MapsetVerifier.Checks.Utils;

/// <summary> Finds runs of notes and measures their colours and scroll speed in osu!taiko difficulties. </summary>
public static class TaikoPatterns
{
    /// <summary>
    ///     How wide a regular note is, in the units slider velocity is measured in (a beat is 140 of these wide per
    ///     1.00x slider velocity). Notes are 0.475 of the playfield height, which is 125 units.
    /// </summary>
    private const double NoteDiameter = 59;

    /// <summary> How much further osu!taiko notes scroll per beat than the length of a slider of one beat. </summary>
    private const double ScrollMultiplier = 1.4;

    /// <summary>
    ///     Returns the runs of at least <paramref name="minNotes" /> notes which are all at most the given part of
    ///     a beat apart, scaled by BPM (see <see cref="TaikoUtils.GetNormalizedMsPerBeat" />). Drumrolls and
    ///     spinners end a run.
    /// </summary>
    public static IEnumerable<List<HitObject>> GetPatterns(
        Beatmap beatmap,
        double beats,
        int minNotes
    )
    {
        var pattern = new List<HitObject>();

        foreach (var hitObject in beatmap.HitObjects)
        {
            if (
                pattern.Count > 0
                && (hitObject is not Circle || !IsWithin(beatmap, pattern[^1], hitObject, beats))
            )
            {
                if (pattern.Count >= minNotes)
                    yield return pattern;

                pattern = [];
            }

            // Notes on top of each other are hit as one, another check reports those.
            if (
                hitObject is Circle
                && (pattern.Count == 0 || hitObject.time - pattern[^1].time >= 1)
            )
                pattern.Add(hitObject);
        }

        if (pattern.Count >= minNotes)
            yield return pattern;
    }

    /// <summary>
    ///     Returns how many notes in a row have the same colour, in order. Two dons and a kat is [2, 1].
    /// </summary>
    public static List<int> GetColourGroups(IReadOnlyList<HitObject> pattern)
    {
        var groups = new List<int>();

        for (var i = 0; i < pattern.Count; i++)
            if (i > 0 && pattern[i].IsDon() == pattern[i - 1].IsDon())
                groups[^1]++;
            else
                groups.Add(1);

        return groups;
    }

    /// <summary> Returns the colours of the pattern as letters, d for don and k for kat. </summary>
    public static string GetColours(IEnumerable<HitObject> pattern) =>
        string.Concat(pattern.Select(note => note.IsDon() ? 'd' : 'k'));

    /// <summary>
    ///     Returns how fast the note scrolls, as beats per minute times slider velocity, so that slider velocity
    ///     which only evens out a BPM change counts as the same speed.
    /// </summary>
    public static double GetScrollSpeed(HitObject note)
    {
        var line = note.beatmap.GetSliderVelocityLine(note.time);
        if (line == null)
            return 0;

        // The line may be up to a millisecond later than the note, see GetSliderVelocityLine.
        var timing = note.beatmap.GetTimingLine<UninheritedLine>(Math.Max(note.time, line.Offset));

        return timing == null
            ? 0
            : timing.bpm * line.SvMult * note.beatmap.DifficultySettings.sliderMultiplier;
    }

    /// <summary> Returns whether the notes would overlap if both scrolled at the given speed. </summary>
    public static bool Overlaps(HitObject first, HitObject second, double scrollSpeed) =>
        (second.time - first.time) / 60000 * scrollSpeed * 100 * ScrollMultiplier < NoteDiameter;

    private static bool IsWithin(Beatmap beatmap, HitObject first, HitObject second, double beats)
    {
        var line = beatmap.GetTimingLine<UninheritedLine>(first.time);

        return line != null
            && second.time - first.GetEndTime()
                <= beats * line.GetNormalizedMsPerBeat() + Common.MS_EPSILON;
    }
}
