using System.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

/// <summary>
/// Class which mimics the behaviour the game has when doing hyperdash calculations to make catch specific checks easier to make.
/// </summary>
public static class HitObjectDistanceCalculator
{
    /// <summary>
    /// The width of the catcher which can receive fruit. Equivalent to "catchMargin" in osu-stable.
    /// </summary>
    private const float AllowedCatchRange = 0.8f;

    /// <summary>
    /// The size of the catcher at 1x scale.
    /// </summary>
    private const float BaseSize = 106.75f;

    /// <summary>
    /// The speed of the catcher when the catcher is dashing.
    /// </summary>
    private const double BaseDashSpeed = 1.0;

    /// <summary>
    /// 1/4th of a frame of grace time, taken from osu-stable
    /// </summary>
    private static double QuarterFrameGrace => 1000.0 / 60.0 / 4;

    private static float CalculateCatchWidth(float circleSize) => BaseSize * Math.Abs(CalculateScale(circleSize).X) * AllowedCatchRange;

    private static float CalculateScaleFromCircleSize(float circleSize)
    {
        // (1 - 0.7 * difficultyRange) / 2 per osu!catch sizing.
        return (float)(1.0 - 0.7 * DifficultyRange(circleSize)) / 2f;
    }

    private static double DifficultyRange(double difficulty) => (difficulty - 5) / 5.0;

    /// <summary>
    /// Calculates the scale of the catcher based off the provided beatmap difficulty.
    /// </summary>
    private static Vector2 CalculateScale(float circleSize) => new(CalculateScaleFromCircleSize(circleSize) * 2);

    /// <summary>
    /// Calculates movement metadata (dash/hyper distances, direction, edge movement) between sequential objects.
    /// </summary>
    public static void CalculateDistances(List<ICatchHitObject> allObjects, Beatmap beatmap)
    {
        if (allObjects.Count < 2) return;
        allObjects.Sort((h1, h2) => h1.Time.CompareTo(h2.Time));

        var catcherWidth = CalculateCatchWidth(beatmap.DifficultySettings.circleSize);
        var halfCatcherWidth = catcherWidth * 0.5f;
        halfCatcherWidth /= AllowedCatchRange;

        var lastDirection = CatchNoteDirection.None;
        double dashRange = halfCatcherWidth;
        
        // We need to consider slider parts as well for movement calculation
        // Add all to a single array so we can iterate through them in order
        var allObjectsPlusParts = new List<ICatchHitObject>();
        foreach (var obj in allObjects)
        {
            allObjectsPlusParts.Add(obj);
            if (obj is JuiceStream js)
                allObjectsPlusParts.AddRange(js.Parts);
        }

        for (var i = 0; i < allObjectsPlusParts.Count - 1; i++)
        {
            var current = allObjectsPlusParts[i];
            var next = allObjectsPlusParts[i + 1];
            current.Target = next;

            // Spinner gap logic now polymorphic
            if (current is Bananas || next is Bananas)
            {
                // TODO support spinner hyperdashes, although they are very rarely used
                current.MovementType = CatchMovementType.Walk;
                current.DistanceToHyper = float.PositiveInfinity;

                // Reset everything when we have a spinner, ignore spinner hyperdashes
                dashRange = halfCatcherWidth;
                lastDirection = CatchNoteDirection.None;
                continue;
            }

            var direction = next.Position.X > current.Position.X ? CatchNoteDirection.Left : CatchNoteDirection.Right;
            var timeToNext = next.Time - current.Time - QuarterFrameGrace;
            var distance = Math.Abs(next.Position.X - current.Position.X);

            var dashDistanceToNext = distance - (lastDirection == direction ? dashRange : halfCatcherWidth);
            current.DistanceToHyper = (float)(timeToNext * BaseDashSpeed - dashDistanceToNext);

            current.MovementType = current.DistanceToHyper < 0
                ? CatchMovementType.Hyperdash 
                : CatchMovementType.Walk;

            dashRange = current.MovementType == CatchMovementType.Hyperdash
                ? halfCatcherWidth
                : Math.Clamp(current.DistanceToHyper, 0, halfCatcherWidth);

            current.NoteDirection = direction;

            lastDirection = current.NoteDirection;
        }
    }
}