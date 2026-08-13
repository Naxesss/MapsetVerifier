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
    /// The speed of the catcher when the catcher is walking, which is half of the dash speed.
    /// </summary>
    private const double BaseWalkSpeed = 0.5;

    /// <summary>
    /// 1/4th of a frame of grace time, taken from osu-stable
    /// </summary>
    private static double QuarterFrameGrace => 1000.0 / 60.0 / 4;

    private static float CalculateCatchWidth(float circleSize) =>
        BaseSize * Math.Abs(CalculateScale(circleSize).X) * AllowedCatchRange;

    private static float CalculateScaleFromCircleSize(float circleSize)
    {
        // (1 - 0.7 * difficultyRange) / 2 per osu!catch sizing.
        return (float)(1.0 - 0.7 * DifficultyRange(circleSize)) / 2f;
    }

    private static double DifficultyRange(double difficulty) => (difficulty - 5) / 5.0;

    /// <summary>
    /// Calculates the scale of the catcher based off the provided beatmap difficulty.
    /// </summary>
    private static Vector2 CalculateScale(float circleSize) =>
        new(CalculateScaleFromCircleSize(circleSize) * 2);

    /// <summary>
    /// Calculates movement metadata (dash/hyper distances, direction, edge movement) between sequential objects.
    /// </summary>
    /// <remarks>
    /// Hyperdashes mimic the game exactly, since they are an actual gameplay mechanic.
    ///
    /// Dashes are not a game mechanic, so they are determined by asking whether the pattern can be
    /// played while only walking. The range of catcher positions that can still catch everything so
    /// far is carried along: it widens by how far the catcher can walk in the available time and is
    /// then narrowed to the positions that catch the next object. Once that range becomes empty,
    /// walking can no longer keep up and a dash is required.
    ///
    /// The range starts at the object itself rather than anywhere the catcher could still catch it,
    /// so a movement on its own allows one catcher width of leniency, matching how the game and the
    /// ranking criteria measure a dash. Carrying the range instead of comparing a single pair at a
    /// time additionally resolves patterns that only drift away slowly, such as a zigzag that creeps
    /// sideways, which a per-pair comparison cannot see because it would hand out that leniency again
    /// on every direction change.
    /// </remarks>
    public static void CalculateDistances(List<ICatchHitObject> allObjects, Beatmap beatmap)
    {
        if (allObjects.Count < 2)
            return;
        allObjects.Sort((h1, h2) => h1.Time.CompareTo(h2.Time));

        var catcherWidth = CalculateCatchWidth(beatmap.DifficultySettings.circleSize);

        // The distance from the middle of the catcher within which a fruit is actually caught, which
        // is the leniency a player really has when deciding whether they can walk a movement.
        var halfCatchWidth = catcherWidth * 0.5f;

        // Hyperdash triggering undoes the catch range to get the full catcher size back, a quirk
        // taken from osu-stable. Only the hyperdash calculation may use this wider value.
        var halfHyperWidth = halfCatchWidth / AllowedCatchRange;

        var lastDirection = CatchNoteDirection.None;
        double dashRange = halfHyperWidth;

        // The range of catcher positions that are still reachable while only walking. Empty means
        // walking can no longer keep up and the player has to dash. See CalculateDistances remarks.
        var walkRangeSet = false;
        double walkRangeLow = 0;
        double walkRangeHigh = 0;

        // We need to consider slider parts as well for movement calculation
        // Add all to a single array so we can iterate through them in order
        var allObjectsPlusParts = new List<ICatchHitObject>();
        foreach (var obj in allObjects)
        {
            allObjectsPlusParts.Add(obj);
            if (obj is JuiceStream js)
                allObjectsPlusParts.AddRange(js.Parts);
        }

        for (var i = 0; i < allObjectsPlusParts.Count; i++)
        {
            var current = allObjectsPlusParts[i];
            var next = i < allObjectsPlusParts.Count - 1 ? allObjectsPlusParts[i + 1] : null;

            if (next == null)
            {
                // No next object means this is the last object of the map
                current.MovementType = CatchMovementType.Walk;
                current.NoteDirection = CatchNoteDirection.None;
                current.DistanceToHyper = float.PositiveInfinity;
                current.DistanceToDash = float.PositiveInfinity;
                continue;
            }

            // Spinner gap logic now polymorphic
            if (current is Bananas || next is Bananas)
            {
                // TODO support spinner hyperdashes, although they are very rarely used
                current.MovementType = CatchMovementType.Walk;
                current.NoteDirection = CatchNoteDirection.None;
                current.DistanceToHyper = float.PositiveInfinity;
                current.DistanceToDash = float.PositiveInfinity;

                // Reset everything when we have a spinner, ignore spinner hyperdashes
                dashRange = halfHyperWidth;
                walkRangeSet = false;
                lastDirection = CatchNoteDirection.None;
                continue;
            }

            var currentX = (int)current.Position.X;
            var nextX = (int)next.Position.X;

            CatchNoteDirection direction;
            if (currentX == nextX)
            {
                // Standstill, no direction
                direction = CatchNoteDirection.None;
            }
            else if (currentX > nextX)
            {
                direction = CatchNoteDirection.Left;
            }
            else
            {
                direction = CatchNoteDirection.Right;
            }

            var timeToNext = next.Time - current.Time - QuarterFrameGrace;
            var distance = Math.Abs(next.Position.X - current.Position.X);

            var dashDistanceToNext =
                distance - (lastDirection == direction ? dashRange : halfHyperWidth);
            current.DistanceToHyper = (float)(timeToNext * BaseDashSpeed - dashDistanceToNext);

            var walkReach = Math.Max(0, timeToNext) * BaseWalkSpeed;

            // A single movement is walkable when the catcher can cover it using the part of the
            // catcher that actually catches fruit plus however far it walks in the available time.
            var movementMargin = halfCatchWidth + walkReach - distance;

            if (!walkRangeSet)
            {
                walkRangeLow = current.Position.X - halfCatchWidth;
                walkRangeHigh = current.Position.X + halfCatchWidth;
                walkRangeSet = true;
            }

            // Positions that catch the next object, and the positions we can still walk to in time.
            var targetLow = next.Position.X - halfCatchWidth;
            var targetHigh = next.Position.X + halfCatchWidth;
            var walkedLow = walkRangeLow - walkReach;
            var walkedHigh = walkRangeHigh + walkReach;

            // How many pixels the next object may still move before those two no longer overlap.
            // On its own this only catches drift, since a quiet section lets the range grow back to
            // the full catcher width, so the stricter of the two measurements decides.
            var chainMargin = Math.Min(targetHigh - walkedLow, walkedHigh - targetLow);

            current.DistanceToDash = (float)Math.Min(movementMargin, chainMargin);

            var canWalk = current.DistanceToDash >= 0;

            // A hyperdash always requires dashing as well, so it takes precedence.
            if (current.DistanceToHyper < 0)
                current.MovementType = CatchMovementType.Hyperdash;
            else if (!canWalk)
                current.MovementType = CatchMovementType.Dash;
            else
                current.MovementType = CatchMovementType.Walk;

            dashRange =
                current.MovementType == CatchMovementType.Hyperdash
                    ? halfHyperWidth
                    : Math.Clamp(current.DistanceToHyper, 0, halfHyperWidth);

            if (canWalk)
            {
                walkRangeLow = Math.Max(walkedLow, targetLow);
                walkRangeHigh = Math.Min(walkedHigh, targetHigh);
            }
            else
            {
                // The player dashed, so redo the same step at dashing speed to keep the chain going.
                var dashReach = Math.Max(0, timeToNext) * BaseDashSpeed;
                walkRangeLow = Math.Max(walkRangeLow - dashReach, targetLow);
                walkRangeHigh = Math.Min(walkRangeHigh + dashReach, targetHigh);

                // Beyond even dashing range, so nothing can be carried over.
                if (walkRangeLow > walkRangeHigh)
                    walkRangeLow = walkRangeHigh = next.Position.X;
            }

            current.NoteDirection = direction;

            lastDirection = current.NoteDirection;
        }
    }
}
