namespace MapsetVerifier.Parser.Objects.HitObjects.Mania;

public static class ManiaExtensions
{
    public static int GetColumn(HitObject hitObject, float keys)
    {
        // Mania is rather weird as the X position isn't given in columns but rather pixels
        // Manual changes or certain editors can cause objects to be slightly off from their intended column,
        // so we determine the column the same way the game does, clamping anything outside the playfield
        var columns = Math.Max(1, (int)keys);
        var column = (int)Math.Floor(hitObject.Position.X * columns / 512f);

        return Math.Clamp(column, 0, columns - 1);
    }
}
