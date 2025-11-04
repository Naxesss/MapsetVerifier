using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public static class CatchExtensions
{
    public static List<CatchHitObject> GetCatchHitObjects(this Beatmap? beatmap)
    {
        if (beatmap == null) return [];
        return beatmap.HitObjects
            .Cast<CatchHitObject>()
            .ToList();
    }
    
    public static string GetTimestamps(params CatchHitObject[] catchHitObject)
    {
        var timestampObjects = new List<CatchHitObject>();
        foreach (var hitObject in catchHitObject)
        {
            switch (hitObject.NoteType)
            {
                case CatchNoteType.Circle:
                case CatchNoteType.Head:
                    timestampObjects.Add(hitObject);
                    break;
                case CatchNoteType.Droplet:
                case CatchNoteType.Repeat:
                case CatchNoteType.Tail:
                    if (hitObject.SliderHead != null && !timestampObjects.Contains(hitObject.SliderHead))
                    {
                        timestampObjects.Add(hitObject.SliderHead);
                    }
                    break;
                // Ignore spinners for timestamp purposes
                case CatchNoteType.Spinner:
                default:
                    break;
            }
        }

        return Timestamp.Get(timestampObjects.Select(x => x.Original).ToArray());
    }
}