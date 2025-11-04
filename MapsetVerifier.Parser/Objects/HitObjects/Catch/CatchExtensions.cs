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
}