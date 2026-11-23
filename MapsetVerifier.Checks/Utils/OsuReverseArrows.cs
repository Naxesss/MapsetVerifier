using System.Globalization;
using MapsetVerifier.Parser.Objects.HitObjects;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using Vector2 = osuTK.Vector2;

namespace MapsetVerifier.Checks.Utils;

/// <summary>
///     Compares where the reverse arrows of osu! sliders point with where the slider body goes. The game
///     points an arrow along the very last piece of the path, however short, so a tiny hook at the end
///     turns the arrow while the slider itself looks the same.
/// </summary>
public static class OsuReverseArrows
{
    /// <summary> Arrows further off than this, in degrees, don't follow the path. </summary>
    private const double MaxAngle = 45;

    /// <summary>
    ///     The angle changes by some 15 degrees depending on where the path is said to come out, so arrows up to
    ///     this far off are too close to the limit to be sure about.
    /// </summary>
    private const double ClearAngle = 60;

    /// <summary> Arrows more than a right angle off point away from where the slider goes. </summary>
    private const double AwayAngle = 90;

    /// <summary>
    ///     How far from the middle of the arrow the path comes out, in circle radii. It may already be seen
    ///     somewhat before the edge of the circle, so the arrow has to be off at both distances.
    /// </summary>
    private const float NearDistance = 0.75f;

    private const float FarDistance = 1;

    /// <summary>
    ///     The circles at both ends of a slider that doesn't get further away than this, in circle radii, overlap,
    ///     so there's hardly any path to see in between.
    /// </summary>
    private const float ClearDistance = 2;

    /// <summary> The angle between an arrow and the path, in degrees, and whether it's clearly a wrong one. </summary>
    public readonly record struct Misdirection(double Angle, bool IsClear);

    /// <summary>
    ///     Returns the worst reverse arrow of the slider not following its visible path, if any. Sliders that stay
    ///     beneath the circle with the arrow have no visible path to follow.
    /// </summary>
    public static Misdirection? GetMisdirection(Slider slider)
    {
        if (slider.EdgeAmount < 2)
            return null;

        var path = GetPath(slider);
        var radius = slider.beatmap.DifficultySettings.GetCircleRadius();

        // The first reverse is at the end of the path. A second one is back at the start.
        var end = GetMisdirection(Enumerable.Reverse(path).ToList(), radius);
        var start = slider.EdgeAmount > 2 ? GetMisdirection(path, radius) : null;

        return new[] { end, start }
            .Where(misdirection => misdirection != null)
            .OrderByDescending(misdirection => misdirection!.Value.IsClear)
            .ThenByDescending(misdirection => misdirection!.Value.Angle)
            .FirstOrDefault();
    }

    private static Misdirection? GetMisdirection(List<Vector2> path, float radius)
    {
        if (
            GetAngle(path, radius * NearDistance) is not { } near
            || GetAngle(path, radius * FarDistance) is not { } far
        )
            return null;

        // Arrows that are only off at one of the distances still point at some of the visible path.
        var angle = Math.Min(near, far);

        if (angle <= MaxAngle)
            return null;

        var isLong = path.Any(point => (point - path[0]).Length >= radius * ClearDistance);

        return new Misdirection(angle, angle > (isLong ? ClearAngle : AwayAngle));
    }

    /// <summary>
    ///     Angle between the first piece of a path and the point where it gets the given distance away. Null if it
    ///     never does.
    /// </summary>
    private static double? GetAngle(List<Vector2> path, float distance)
    {
        if (path.Count < 2)
            return null;

        var origin = path[0];
        // Same as the game, which looks for the closest point that isn't in the same place.
        var arrowIndex = path.FindIndex(point => !Precision.AlmostEquals(point, origin));
        var visibleIndex = path.FindIndex(point => (point - origin).Length >= distance);

        if (arrowIndex < 1 || visibleIndex < 1)
            return null;

        var visiblePoint = GetPointAtDistance(
            path[visibleIndex - 1],
            path[visibleIndex],
            origin,
            distance
        );

        var cosine = Vector2.Dot(
            (path[arrowIndex] - origin).Normalized(),
            (visiblePoint - origin).Normalized()
        );

        return Math.Acos(Math.Clamp(cosine, -1, 1)) * 180 / Math.PI;
    }

    /// <summary> Where a line from within the given distance of the origin to outside of it crosses it. </summary>
    private static Vector2 GetPointAtDistance(
        Vector2 inside,
        Vector2 outside,
        Vector2 origin,
        float distance
    )
    {
        for (var i = 0; i < 20; i++)
        {
            var middle = (inside + outside) / 2;

            if ((middle - origin).Length < distance)
                inside = middle;
            else
                outside = middle;
        }

        return outside;
    }

    /// <summary>
    ///     The path exactly as the game draws it, relative to the start of the slider. Less precise paths, such
    ///     as <see cref="Slider.PathPxPositions" />, don't have the tiny pieces that decide on the arrow.
    /// </summary>
    internal static List<Vector2> GetPath(Slider slider)
    {
        var path = new SliderPath(GetControlPoints(slider).ToArray(), slider.PixelLength);
        var points = new List<Vector2>();

        path.GetPathToProgress(points, 0, 1);

        return points;
    }

    /// <summary>
    ///     Reads the curves of a slider like the game does: every letter starts a new curve, as does a node
    ///     in the same place as the one before it.
    /// </summary>
    private static List<PathControlPoint> GetControlPoints(Slider slider)
    {
        var start = slider.UnstackedPosition;
        var origin = new Vector2(start.X, start.Y);
        var segments = new List<(PathType Type, List<Vector2> Points)>();

        foreach (var part in slider.code.Split(',')[5].Split('|'))
        {
            if (part.Length > 0 && char.IsLetter(part[0]))
            {
                segments.Add((GetPathType(part), segments.Count == 0 ? [Vector2.Zero] : []));
                continue;
            }

            var node = part.Split(':');

            if (node.Length < 2 || segments.Count == 0)
                continue;

            segments[^1]
                .Points.Add(
                    new Vector2(
                        (int)double.Parse(node[0], CultureInfo.InvariantCulture),
                        (int)double.Parse(node[1], CultureInfo.InvariantCulture)
                    ) - origin
                );
        }

        segments.RemoveAll(segment => segment.Points.Count == 0);

        var controlPoints = new List<PathControlPoint>();

        for (var i = 0; i < segments.Count; i++)
        {
            var (type, points) = segments[i];

            if (type == PathType.PERFECT_CURVE)
            {
                // A curve ends where the next one starts.
                var all = i + 1 < segments.Count ? [.. points, segments[i + 1].Points[0]] : points;

                if (all.Count != 3)
                    type = PathType.BEZIER;
                else if (IsLinear(all[0], all[1], all[2]))
                    type = PathType.LINEAR;
            }

            controlPoints.Add(new PathControlPoint(points[0], type));

            for (var j = 1; j < points.Count; j++)
            {
                // The first of two nodes in the same place starts the new curve, and the second isn't needed.
                // Catmull curves are always one curve, and a double node at the end has nothing to start.
                if (
                    j > 1
                    && j < points.Count - 1
                    && points[j] == points[j - 1]
                    && type != PathType.CATMULL
                )
                {
                    controlPoints[^1].Type = type;
                    continue;
                }

                controlPoints.Add(new PathControlPoint(points[j]));
            }
        }

        return controlPoints;
    }

    private static bool IsLinear(Vector2 a, Vector2 b, Vector2 c) =>
        Precision.AlmostEquals(0, (b.Y - a.Y) * (c.X - a.X) - (b.X - a.X) * (c.Y - a.Y));

    private static PathType GetPathType(string text) =>
        text[0] switch
        {
            'B' when int.TryParse(text[1..], out var degree) && degree > 0 => PathType.BSpline(
                degree
            ),
            'B' => PathType.BEZIER,
            'L' => PathType.LINEAR,
            'P' => PathType.PERFECT_CURVE,
            _ => PathType.CATMULL,
        };
}
