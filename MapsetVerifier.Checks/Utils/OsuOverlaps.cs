using System.Numerics;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Finds the overlaps the osu! difficulty guidelines talk about: slider bodies hidden under an earlier
///     slider, and circles, slider heads and slider tails overlapping each other.
/// </summary>
public static class OsuOverlaps
{
    /// <summary> A gap of at most this many beats of the song is a 1/2 rhythm or faster. </summary>
    private const double MaxHalfBeats = 0.6;

    /// <summary>
    ///     How far, in circle radii, a slider body may stick out from under another and still count as
    ///     fully overlapped. A little more than the width of the slider border.
    /// </summary>
    private const float MaxBodyStickOut = 0.25f;

    /// <summary> How close, in circle radii, both ends of two sliders are for them to be a stack. </summary>
    private const float MaxStackedEndDistance = 0.5f;

    /// <summary>
    ///     Distance, in circle diameters, below which an object overlaps the one right before it. Objects
    ///     which only touch at their borders are left out.
    /// </summary>
    private const float MaxConsecutiveDistance = 0.9f;

    /// <summary> Same, for other pairs. Below this one covers more than half of the other. </summary>
    private const float MaxOtherDistance = 0.35f;

    /// <summary> The game stacks objects closer together than this. </summary>
    private const float MaxStackedPx = 3;

    private const double PathStepPx = 4;
    private const int MaxPathSteps = 500;

    /// <summary> A slider whose body is hidden under an earlier one, and the time between the two. </summary>
    public readonly record struct BodyOverlap(Slider Covering, Slider Covered, double GapMs)
    {
        public double ReferenceBeats => GapMs / RhythmUtils.ReferenceBeatMs;

        /// <summary> Whether the gap is at most this long, with 1 ms of leniency for rounding. </summary>
        public bool IsWithin(double referenceBeats) =>
            GapMs <= referenceBeats * RhythmUtils.ReferenceBeatMs + 1;
    }

    /// <summary> Two objects with overlapping circles, heads or tails. </summary>
    public readonly record struct EndOverlap(HitObject First, HitObject Second);

    /// <summary>
    ///     Sliders whose body fully lies on an earlier slider that ended at most the given number of 180 BPM
    ///     beats before. Earlier objects are drawn on top, so the body of the later one can't be seen, only
    ///     its head and tail. Sliders stacked on a copy of themselves are left out, as are sliders that only
    ///     appear once the earlier one is gone. At most one overlap is returned for each covered slider.
    /// </summary>
    public static IEnumerable<BodyOverlap> BodyOverlaps(Beatmap beatmap, double maxReferenceBeats)
    {
        var objects = beatmap.HitObjects;
        var radius = beatmap.DifficultySettings.GetCircleRadius();
        var tolerance = radius * MaxBodyStickOut;
        // Despite the name, this is how long an object is on screen before it's hit.
        var visibleMs = beatmap.DifficultySettings.GetFadeInTime();
        var paths = new Dictionary<Slider, List<Vector2>>();

        for (var i = 1; i < objects.Count; ++i)
        {
            // Sliders this short have no body to speak of.
            if (objects[i] is not Slider covered || covered.PixelLength < tolerance * 2)
                continue;

            for (var j = i - 1; j >= 0; --j)
            {
                var gapMs = covered.time - objects[j].GetEndTime();

                if (gapMs >= visibleMs)
                    break;

                if (objects[j] is not Slider covering)
                    continue;

                var overlap = new BodyOverlap(covering, covered, gapMs);

                if (!overlap.IsWithin(maxReferenceBeats))
                    break;

                if (!IsBodyCovered(covering, covered, tolerance, radius, GetPath))
                    continue;

                yield return overlap;
                break;
            }
        }

        yield break;

        List<Vector2> GetPath(Slider slider) =>
            paths.TryGetValue(slider, out var path) ? path : paths[slider] = SamplePath(slider);
    }

    /// <summary>
    ///     Pairs of objects whose circles, heads or tails overlap while both are on screen, meant for
    ///     Easies and Normals. Objects 1/2 apart or less overlap because of distance snapping, so their own
    ///     gap is left out, and other pairs only count if one covers more than half of the other. Visible stacks
    ///     are left out too.
    /// </summary>
    public static IEnumerable<EndOverlap> EndOverlaps(Beatmap beatmap)
    {
        var objects = beatmap.HitObjects;
        var diameter = beatmap.DifficultySettings.GetCircleRadius() * 2;
        // Despite the name, this is how long an object is fully opaque before it's hit.
        var opaqueMs = beatmap.DifficultySettings.GetPreemptTime();

        for (var i = 1; i < objects.Count; ++i)
        {
            var second = objects[i];

            if (second is Spinner)
                continue;

            var secondPoints = GetEndPoints(second);

            for (var j = i - 1; j >= 0; --j)
            {
                var first = objects[j];

                if (first is Spinner || second.time - first.GetEndTime() >= opaqueMs)
                    break;

                var isConsecutive = j == i - 1;

                if (
                    AnyOverlap(
                        beatmap,
                        first,
                        second,
                        secondPoints,
                        isConsecutive,
                        diameter,
                        opaqueMs
                    )
                )
                    yield return new EndOverlap(first, second);
            }
        }
    }

    private static bool AnyOverlap(
        Beatmap beatmap,
        HitObject first,
        HitObject second,
        List<EndPoint> secondPoints,
        bool isConsecutive,
        float diameter,
        double opaqueMs
    )
    {
        foreach (var firstPoint in GetEndPoints(first))
        {
            // Circles and heads disappear once hit, so they can't overlap what appears after.
            if (second.time - firstPoint.GoneTime >= opaqueMs)
                continue;

            foreach (var secondPoint in secondPoints)
            {
                // Where the player goes from the first object to the second.
                var isMovement = isConsecutive && firstPoint.IsEnd && secondPoint.IsHead;

                if (isMovement && IsHalfOrFaster(beatmap, first.GetEndTime(), second.time))
                    continue;

                if (IsVisibleStack(firstPoint, secondPoint))
                    continue;

                var maxDistance = isMovement ? MaxConsecutiveDistance : MaxOtherDistance;

                if (
                    Vector2.Distance(firstPoint.Position, secondPoint.Position)
                    < diameter * maxDistance
                )
                    return true;
            }
        }

        return false;
    }

    /// <summary> Whether the points are in the same place and the game shows them as a stack. </summary>
    private static bool IsVisibleStack(EndPoint first, EndPoint second) =>
        first.StackOffset != second.StackOffset
        && Vector2.Distance(
            first.Position - first.StackOffset,
            second.Position - second.StackOffset
        ) < MaxStackedPx;

    private readonly record struct EndPoint(
        Vector2 Position,
        Vector2 StackOffset,
        double GoneTime,
        bool IsHead,
        bool IsEnd
    );

    /// <summary> The head of an object, and the tail if it's a slider. </summary>
    private static List<EndPoint> GetEndPoints(HitObject hitObject)
    {
        if (hitObject is not Slider slider)
        {
            var offset = hitObject is Stackable stackable ? GetStackOffset(stackable) : default;
            return [new EndPoint(hitObject.Position, offset, hitObject.time, true, true)];
        }

        var endTime = slider.GetEndTime();
        var endsOnTail = slider.EdgeAmount % 2 == 1;
        var stackOffset = GetStackOffset(slider);
        // Reversed sliders come back to their head.
        var headGoneTime = slider.EdgeAmount == 1 ? slider.time : endTime;

        return
        [
            new EndPoint(slider.Position, stackOffset, headGoneTime, true, !endsOnTail),
            new EndPoint(GetTailPosition(slider), stackOffset, endTime, false, endsOnTail),
        ];
    }

    /// <summary> Whether a gap is 1/2 or faster, either in the song itself or at 180 BPM. </summary>
    private static bool IsHalfOrFaster(Beatmap beatmap, double start, double end)
    {
        var gapMs = end - start;

        if (!OsuPatterns.IsRest(gapMs))
            return true;

        var line = beatmap.GetTimingLine<UninheritedLine>(end);

        return line != null && gapMs <= line.msPerBeat * MaxHalfBeats;
    }

    private static bool IsBodyCovered(
        Slider covering,
        Slider covered,
        float tolerance,
        float radius,
        Func<Slider, List<Vector2>> getPath
    )
    {
        // Cheap way to tell that the head is too far away, as for most sliders.
        var headDistance = Vector2.Distance(covering.Position, covered.Position);

        if (!(headDistance <= covering.PixelLength + tolerance))
            return false;

        var coveringPath = getPath(covering);

        if (!IsOnPath(covered.Position, coveringPath, tolerance))
            return false;

        var coveredPath = getPath(covered);

        // Starting with the tail, as that's the most likely part to stick out.
        for (var i = coveredPath.Count - 1; i >= 0; --i)
            if (!IsOnPath(coveredPath[i], coveringPath, tolerance))
                return false;

        var isStack =
            headDistance <= radius * MaxStackedEndDistance
            && Vector2.Distance(coveringPath[^1], coveredPath[^1])
                <= radius * MaxStackedEndDistance;

        return !isStack;
    }

    private static bool IsOnPath(Vector2 point, List<Vector2> path, float tolerance)
    {
        var toleranceSquared = tolerance * tolerance;

        for (var i = 1; i < path.Count; ++i)
            if (DistanceSquaredToSegment(point, path[i - 1], path[i]) <= toleranceSquared)
                return true;

        return false;
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();

        if (lengthSquared <= 0)
            return Vector2.DistanceSquared(point, start);

        var fraction = Math.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0, 1);

        return Vector2.DistanceSquared(point, start + segment * fraction);
    }

    /// <summary>
    ///     The path of a slider as it's shown, including stacking. Much coarser than
    ///     <see cref="Slider.PathPxPositions" />, which would be kept in memory for every slider compared.
    /// </summary>
    private static List<Vector2> SamplePath(Slider slider)
    {
        var steps = (int)Math.Clamp(Math.Ceiling(slider.PixelLength / PathStepPx), 1, MaxPathSteps);
        var duration = slider.GetCurveDuration();
        var stackOffset = GetStackOffset(slider);
        var path = new List<Vector2>(steps + 1) { slider.Position };

        for (var i = 1; i < steps; ++i)
            path.Add(slider.GetPathPosition(slider.time + duration * i / steps) + stackOffset);

        path.Add(GetTailPosition(slider));

        return path;
    }

    /// <summary> Where the tail of the slider is shown, which isn't where it ends if it's reversed. </summary>
    private static Vector2 GetTailPosition(Slider slider) =>
        slider.EdgeAmount % 2 == 1
            ? slider.EndPosition
            : slider.GetPathPosition(slider.time + slider.GetCurveDuration())
                + GetStackOffset(slider);

    private static Vector2 GetStackOffset(Stackable stackable) =>
        stackable.Position - stackable.UnstackedPosition;
}
