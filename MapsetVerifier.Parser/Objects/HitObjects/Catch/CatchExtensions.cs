using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public static class CatchExtensions
{
    public static List<ICatchHitObject> GetCatchHitObjects(this Beatmap? beatmap)
    {
        if (beatmap == null) return [];
        return beatmap.HitObjects
            .Cast<ICatchHitObject>()
            .ToList();
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
                // Ignore spinners for timestamps
            }
        }

        return Timestamp.Get(timestampObjects.Select(x => x.Original).ToArray());
    }
}