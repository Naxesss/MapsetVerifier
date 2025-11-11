using System.Globalization;
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
    public static List<ICatchHitObject> GetCatchHitObjects(this Beatmap? beatmap, bool includeJuiceStreamParts = false)
    {
        if (beatmap == null) return [];
        
        var result = new List<ICatchHitObject>();
        
        foreach (var obj in beatmap.HitObjects)
        {
            // This should not be possible but check to be sure.
            if (obj is not ICatchHitObject catchHitObject) continue;
            
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

        var uniqueTimestamps = timestampObjects
            .Cast<HitObject>()
            .Distinct()
            .ToArray();

        return Timestamp.Get(uniqueTimestamps);
    }

    /// <summary>
    /// Lookup the current uninherited timing line at the time of the given catch hit object and return its scaled BPM.
    /// </summary>
    public static double GetScaledBpm(this Beatmap beatmap, ICatchHitObject obj)
    {
        var timingLine = beatmap.GetTimingLine<UninheritedLine>(obj.Time);
        return timingLine.GetScaledBpm();
    }

    public static float GetCurrentTriggerDistance(this ICatchHitObject currentObject)
    {
        return GetTriggerDistance(currentObject) / currentObject.DistanceToHyper;
    }

    public static float GetTriggerDistance(this ICatchHitObject currentObject)
    {
        return GetTriggerDistance(currentObject, CatchMovementType.Hyperdash, currentObject.DistanceToHyper);
    }
        
    private static float GetTriggerDistance(ICatchHitObject currentObject, CatchMovementType movementType, float distanceTo)
    {
        if (currentObject.MovementType != movementType) return 0f;

        var target = currentObject.Target;
        
        // No target means no distance to calculate.
        if (target == null) return 0f;

        var xDistance = currentObject.NoteDirection switch
        {
            CatchNoteDirection.Left => currentObject.Position.X - target.Position.X,
            CatchNoteDirection.Right => target.Position.X - currentObject.Position.X,
            _ => 0f
        };

        if (xDistance > 0f) return xDistance - Math.Abs(distanceTo);

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
    /// <param name="currentObject">The current object which is getting checked</param>
    /// <param name="difficulty">The difficulty we want to check for</param>
    /// <returns>True if the origin object is higher-snapped</returns>
    public static bool IsHigherSnapped(this ICatchHitObject currentObject, Beatmap.Difficulty difficulty)
    {
        var target = currentObject.Target;
        
        // No need to check higher-snapped because there is nothing to compare to
        if (target == null) return false;
        
        var ms = currentObject.Time - target.Time;

        switch (difficulty)
        {
            case Beatmap.Difficulty.Normal:
                return ms is < 250 and >= 125;
            case Beatmap.Difficulty.Hard:
                var upperBound = currentObject.MovementType == CatchMovementType.Hyperdash ? 250 : 125;
                var lowerBound = currentObject.MovementType == CatchMovementType.Hyperdash ? 125 : 62;

                return ms < upperBound && ms >= lowerBound;
            case Beatmap.Difficulty.Insane:
                return ms is < 125 and >= 62;
            default:
                // Other difficulties the higher-snapped concept does not exist
                return false;
        }
    }
}