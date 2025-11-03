namespace MapsetVerifier.Parser.Objects.HitObjects.Mania;

public static class ManiaExtensions
{
    public static int GetColumn(HitObject hitObject, float keys)
    {
        // Mania is rather weird as the X position isn't given in columns but rather pixels
        // Manual changes or certain editors can cause objects to be slightly off from their intended column
        return (int)hitObject.Position.X / (512 / (int)keys);
    }
}