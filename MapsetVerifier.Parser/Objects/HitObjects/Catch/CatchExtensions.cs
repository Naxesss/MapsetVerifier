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
                case JuiceStream.JuiceStreamPart:
                    timestampObjects.Add(hitObject);
                    break;
                // Ignore spinners for timestamps
            }
        }

        var uniqueTimestamps = timestampObjects
            .Select(x => x.Original)
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
}