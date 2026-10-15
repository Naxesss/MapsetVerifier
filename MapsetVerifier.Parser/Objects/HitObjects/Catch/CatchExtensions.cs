using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public static class CatchExtensions
{
    /// <summary>
    /// Get all catch hit objects from a beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap we want to get the CatchHitObjects for.</param>
    /// <param name="includeJuiceStreamParts">When true it adds all slider parts as seperate objects to the result list.</param>
    /// <returns>A list containing all catch hit objects.</returns>
    public static List<ICatchHitObject> GetCatchHitObjects(
        this Beatmap? beatmap,
        bool includeJuiceStreamParts
    )
    {
        if (beatmap == null)
            return [];

        var result = new List<ICatchHitObject>();

        foreach (var obj in beatmap.HitObjects)
        {
            // This should not be possible but check to be sure.
            if (obj is not ICatchHitObject catchHitObject)
                continue;

            result.Add(catchHitObject);

            if (includeJuiceStreamParts && catchHitObject is JuiceStream juiceStream)
            {
                result.AddRange(juiceStream.Parts);
            }
        }

        return result;
    }

    public static string GetTimestamps(params ICatchHitObject?[] input)
    {
        var timestampObjects = new List<ICatchHitObject>();
        var nonNullHitObjects = input.Where(x => x != null).ToArray();
        foreach (var hitObject in nonNullHitObjects)
        {
            switch (hitObject)
            {
                case Fruit:
                case JuiceStream:
                    timestampObjects.Add(hitObject);
                    break;
                case JuiceStream.JuiceStreamPart juiceStreamPart:
                    // Add the parent when we have a slider part as otherwise we get a wrong starting timestamp
                    timestampObjects.Add(juiceStreamPart.Parent);
                    break;
                // Ignore spinners for timestamps
            }
        }

        var uniqueTimestamps = timestampObjects.Cast<HitObject>().Distinct().ToArray();

        return Timestamp.Get(uniqueTimestamps);
    }

    /// <summary>
    /// Lookup the current uninherited timing line at the time of the given catch hit object and return its scaled BPM.
    /// </summary>
    public static double GetScaledBpm(this Beatmap beatmap, ICatchHitObject obj)
    {
        var timingLine = beatmap.GetTimingLine<UninheritedLine>(obj.Time);
        return timingLine?.GetScaledBpm() ?? 0f;
    }

    public static float GetCurrentTriggerDistance(
        this ICatchHitObject current,
        ICatchHitObject? next
    )
    {
        return GetTriggerDistance(current, next) / current.DistanceToHyper;
    }

    public static float GetTriggerDistance(this ICatchHitObject current, ICatchHitObject? next)
    {
        return GetTriggerDistance(
            current,
            next,
            CatchMovementType.Hyperdash,
            current.DistanceToHyper
        );
    }

    public static float GetCurrentDashTriggerDistance(
        this ICatchHitObject current,
        ICatchHitObject? next
    )
    {
        return GetDashTriggerDistance(current, next) / current.DistanceToDash;
    }

    public static float GetDashTriggerDistance(this ICatchHitObject current, ICatchHitObject? next)
    {
        return GetTriggerDistance(current, next, CatchMovementType.Dash, current.DistanceToDash);
    }

    public static bool IsWalk(this ICatchHitObject current) =>
        current.MovementType is CatchMovementType.Walk;

    private static float GetTriggerDistance(
        ICatchHitObject current,
        ICatchHitObject? next,
        CatchMovementType movementType,
        float distanceTo
    )
    {
        if (current.MovementType != movementType)
            return 0f;

        // No target means no distance to calculate.
        if (next == null)
            return 0f;

        var xDistance = current.NoteDirection switch
        {
            CatchNoteDirection.Left => current.Position.X - next.Position.X,
            CatchNoteDirection.Right => next.Position.X - current.Position.X,
            _ => 0f,
        };

        if (xDistance > 0f)
            return xDistance - Math.Abs(distanceTo);

        return 0f;
    }

    /// <summary>
    /// Check if the snapping between two objects is higher-snapped or basic-snapped
    /// Cup: No dashes or hyperdashes are allowed
    /// Salad: 125-249 ms dashes are higher-snapped, hyperdashes are not allowed
    /// Platter: 62-124 ms dashes are higher-snapped, 125-249 ms hyperdashes are higher-snapped
    /// Rain: 62-124 ms dashes/hyperdashes are higher-snapped
    /// Overdose: No allowed distance are specified so no basic-snapped and higher-snapped exist
    /// </summary>
    /// <param name="current">The current object which is getting checked</param>
    /// <param name="next">The next object that follows</param>
    /// <param name="difficulty">The difficulty we want to check for</param>
    /// <returns>True if the origin object is higher-snapped</returns>
    public static bool IsHigherSnapped(
        this ICatchHitObject current,
        ICatchHitObject next,
        Beatmap.Difficulty difficulty
    )
    {
        var ms = next.Time - current.Time;

        // A walk is never higher-snapped, the concept only applies to dashes and hyperdashes.
        if (current.IsWalk())
        {
            return false;
        }

        switch (difficulty)
        {
            case Beatmap.Difficulty.Normal:
                // Salad only allows dashes, hyperdashes are not allowed at all.
                return current.MovementType == CatchMovementType.Dash && ms is < 250 and >= 125;
            case Beatmap.Difficulty.Hard:
                if (current.MovementType == CatchMovementType.Hyperdash)
                {
                    return ms is < 250 and >= 125;
                }

                return ms is < 125 and >= 62;
            case Beatmap.Difficulty.Insane:
                return ms is < 125 and >= 62;
            default:
                // Other difficulties the higher-snapped concept does not exist
                return false;
        }
    }

    /// <summary>
    /// At 300ms, returns the max distance in pixels
    /// At 180ms returns around half the pixels
    /// At 0ms, returns 0 pixels
    /// Applies a curve to make shorter time between objects result in less distance
    /// </summary>
    /// <param name="ms">The time between the two objects we want to get the curved distance for</param>
    /// <param name="maxDistance">The maximum distance we want to return</param>
    public static float GetCurvedDistance(double ms, float maxDistance)
    {
        // Clamp x to 0–300 range
        var x = MathF.Max(0f, MathF.Min((float)ms, 300f));

        // Apply curve
        return (int)Math.Round(maxDistance * (1f - MathF.Pow(1f - (x / 300f), 0.75f)));
    }

    private const double SnapMargin = 4.0;

    /// <summary>
    /// Check if the current object is the same snap as the other object.
    /// There is a snap margin of 4 ms since objects can be at most 2 ms off before they are detected by MV.
    /// </summary>
    /// <returns>True if the current object and the other object are the same snap</returns>
    public static bool IsSameSnap(ICatchHitObject a, ICatchHitObject b, ICatchHitObject c)
    {
        var timeToB = b.Time - a.Time;
        var timeToC = c.Time - b.Time;

        var snapMin = timeToB - SnapMargin;
        var snapMax = timeToB + SnapMargin;

        return timeToC >= snapMin && timeToC <= snapMax;
    }
}
