using System.Globalization;
using System.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects.Catch;

public static class CatchHitObjectCreator
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
    /// The speed of the catcher when the catcher is not dashing.
    /// </summary>
    private const double BaseWalkSpeed = 0.5;

    /// <summary>
    /// After a hyperdash we want to be more lenient with what the dash distance as player commonly overshoot.
    /// </summary>
    private const double HyperdashLeniency = 0.95;

    /// <summary>Nominal frame rate (stable).</summary>
    private const double FrameRate = 60.0;

    /// <summary>Fraction of a frame used as grace time (1/4 frame).</summary>
    private const double GraceFrameFraction = 0.25;

    /// <summary>Scale applied to convert catcher half width to base walk range.</summary>
    private const double BaseWalkRangeScale = 0.95;

    private static double QuarterFrameGrace => 1000.0 / FrameRate * GraceFrameFraction;

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
    /// Creates CatchHitObjects from base HitObjects (slider parts, circles, spinners) and enriches them with movement data.
    /// </summary>
    public static List<CatchHitObject> CreateCatchHitObjects(Beatmap beatmap)
    {
        var hitObjects = GenerateCatchHitObjects(beatmap);
        CalculateDistances(hitObjects, beatmap);
        return hitObjects; // Already time-ordered by CalculateDistances.
    }

    private static List<CatchHitObject> GenerateCatchHitObjects(Beatmap beatmap)
    {
        var mapObjects = beatmap.HitObjects;
        var objects = new List<CatchHitObject>();

        // Build slider components (head, repeats, tail, droplets)
        foreach (var slider in mapObjects.OfType<Slider>())
        {
            var objectCode = slider.code.Split(',');
            var head = new CatchHitObject(objectCode, beatmap, CatchNoteType.Head, slider);

            var edgeTimes = GetEdgeTimes(slider).ToList();
            for (var i = 0; i < edgeTimes.Count; i++)
            {
                var type = i + 1 == edgeTimes.Count ? CatchNoteType.Tail : CatchNoteType.Repeat;
                objects.Add(CreateObjectExtra(beatmap, head, slider, edgeTimes[i], objectCode, type));
            }

            // Droplets
            foreach (var tickTime in slider.SliderTickTimes)
                objects.Add(CreateObjectExtra(beatmap, head, slider, tickTime, objectCode, CatchNoteType.Droplet));

            objects.Add(head); // Add head last so it appears before repeats (will sort later anyway)
        }

        // Circles
        objects.AddRange(from mapObject in mapObjects where mapObject is Circle select new CatchHitObject(mapObject.code.Split(','), beatmap, CatchNoteType.Circle, mapObject));

        // Spinners (needed for movement gap logic)
        objects.AddRange(from mapObject in mapObjects where mapObject is Spinner select new CatchHitObject(mapObject.code.Split(','), beatmap, CatchNoteType.Spinner, mapObject));

        return objects;
    }

    /// <summary>Get all edge times except the slider head.</summary>
    private static IEnumerable<double> GetEdgeTimes(Slider sObject)
    {
        for (var i = 0; i < sObject.EdgeAmount; ++i)
            yield return sObject.time + sObject.GetCurveDuration() * (i + 1);
    }

    private static CatchHitObject CreateObjectExtra(Beatmap beatmap, CatchHitObject sliderHead,
        Slider slider, double time, string[] objectCode, CatchNoteType type)
    {
        var clone = (string[])objectCode.Clone();
        clone[0] = slider.GetPathPosition(time).X.ToString(CultureInfo.InvariantCulture);
        clone[2] = time.ToString(CultureInfo.InvariantCulture);
        var line = string.Join(",", clone);
        return new CatchHitObject(line.Split(','), beatmap, type, slider) { SliderHead = sliderHead };
    }

    /// <summary>
    /// Calculates movement metadata (dash/hyper distances, direction, edge movement) between sequential objects.
    /// </summary>
    private static void CalculateDistances(List<CatchHitObject> allObjects, Beatmap beatmap)
    {
        if (allObjects.Count < 2) return;

        allObjects.Sort((h1, h2) => h1.time.CompareTo(h2.time));

        var catcherWidth = CalculateCatchWidth(beatmap.DifficultySettings.circleSize);
        var halfCatcherWidth = catcherWidth * 0.5f / AllowedCatchRange;
        var baseWalkRange = halfCatcherWidth * BaseWalkRangeScale;

        var lastDirection = CatchNoteDirection.None;
        double dashRange = halfCatcherWidth;
        var walkRange = baseWalkRange; // TODO: refine for tap dashes (Cup/Salad rules)
        var lastWasHyper = false;

        for (var i = 0; i < allObjects.Count - 1; i++)
        {
            var current = allObjects[i];
            var next = allObjects[i + 1];
            current.Target = next;

            if (current.NoteType == CatchNoteType.Spinner || next.NoteType == CatchNoteType.Spinner)
            {
                current.MovementType = CatchMovementType.Walk;
                dashRange = halfCatcherWidth;
                walkRange = baseWalkRange;
                lastDirection = CatchNoteDirection.None;
                lastWasHyper = false;
                continue;
            }

            var direction = next.X > current.X ? CatchNoteDirection.Left : CatchNoteDirection.Right;
            var timeToNext = (int)next.time - (int)current.time - QuarterFrameGrace;
            var distanceInOsuCoords = Math.Abs(next.X - current.X);

            var actualWalkRange = lastDirection == direction ? walkRange : baseWalkRange;
            if (lastWasHyper) actualWalkRange *= HyperdashLeniency;

            var dashDistanceToNext = distanceInOsuCoords - (lastDirection == direction ? dashRange : halfCatcherWidth);
            current.DistanceToHyper = (float)(timeToNext * BaseDashSpeed - dashDistanceToNext);

            var walkDistanceToNext = distanceInOsuCoords - actualWalkRange;
            current.DistanceToDash = (float)(timeToNext * BaseWalkSpeed - walkDistanceToNext);

            current.MovementType = current.DistanceToHyper < 0 ? CatchMovementType.Hyperdash : current.DistanceToDash < 0 ? CatchMovementType.Dash : CatchMovementType.Walk;
            current.TimeToTarget = timeToNext;

            dashRange = current.MovementType == CatchMovementType.Hyperdash ? halfCatcherWidth : Math.Clamp(current.DistanceToHyper, 0, halfCatcherWidth);
            walkRange = current.MovementType == CatchMovementType.Dash ? baseWalkRange : Math.Clamp(current.DistanceToDash, 0, baseWalkRange);

            // Correctly assign computed direction (previously self-referential)
            current.NoteDirection = direction;
            current.IsEdgeMovement = IsEdgeMovement(beatmap, current);

            lastDirection = current.NoteDirection;
            lastWasHyper = current.MovementType == CatchMovementType.Hyperdash;
        }
    }

    private static double GetBeatsPerMinute(this TimingLine timingLine)
    {
        var msPerBeatString = timingLine.Code.Split(',')[1];
        var msPerBeat = double.Parse(msPerBeatString, CultureInfo.InvariantCulture);
        return 60000 / msPerBeat;
    }

    private static double GetBpm(this Beatmap beatmap, CatchHitObject hitObject)
    {
        var timingLine = beatmap.GetTimingLine(hitObject.time);
        return GetBeatsPerMinute(timingLine);
    }

    private static double GetBpmScale(this Beatmap beatmap, CatchHitObject hitObject) => 180 / beatmap.GetBpm(hitObject);

    private static bool IsEdgeMovement(Beatmap beatmap, CatchHitObject hitObject)
    {
        switch (hitObject.MovementType)
        {
            case CatchMovementType.Walk:
                var comfyDash = 1.44 * beatmap.GetBpm(hitObject); // 1.44 * bpm
                var xDistance = Math.Abs(hitObject.Position.X - hitObject.Target.Position.X);
                return xDistance > comfyDash * beatmap.GetBpmScale(hitObject);
            case CatchMovementType.Dash:
                var pixelScale = 10.0 * beatmap.GetBpmScale(hitObject);
                return hitObject.DistanceToHyper < pixelScale;
            case CatchMovementType.Hyperdash:
            default:
                return false;
        }
    }
}