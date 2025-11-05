using System.Globalization;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;

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
    public static List<ICatchHitObject> CreateCatchHitObjects(Beatmap beatmap, List<HitObject> originalHitObjects)
    {
        var hitObjects = GenerateCatchHitObjects(beatmap, originalHitObjects);
        CalculateDistances(hitObjects, beatmap);
        return hitObjects.OrderBy(o => o.Time).ToList();
    }

    private static List<ICatchHitObject> GenerateCatchHitObjects(Beatmap beatmap, List<HitObject> originalHitObjects)
    {
        var objects = new List<ICatchHitObject>();

        // Sliders -> JuiceStream + parts
        foreach (var slider in originalHitObjects.OfType<Slider>())
        {
            var code = slider.code.Split(',');
            var juiceStream = new JuiceStream(code, beatmap, slider);
            objects.Add(juiceStream);

            // Repeats & tail parts
            var edgeTimes = GetEdgeTimes(slider).ToList();
            for (var i = 0; i < edgeTimes.Count; i++)
            {
                var time = edgeTimes[i];
                var partKind = i + 1 == edgeTimes.Count ? JuiceStream.JuiceStreamPart.PartKind.Tail : JuiceStream.JuiceStreamPart.PartKind.Repeat;
                juiceStream.Parts.Add(CreateJuiceStreamPart(beatmap, juiceStream, slider, time, partKind));
            }
            // Droplets
            foreach (var tickTime in slider.SliderTickTimes)
                juiceStream.Parts.Add(CreateJuiceStreamPart(beatmap, juiceStream, slider, tickTime, JuiceStream.JuiceStreamPart.PartKind.Droplet));

            // We don't add the parts to the result objects as they all get referenced from the juice stream itself
        }

        // Circles -> Fruit
        foreach (var circle in originalHitObjects.OfType<Circle>())
            objects.Add(new Fruit(circle.code.Split(','), beatmap, circle));

        // Spinners -> Bananas
        foreach (var spinner in originalHitObjects.OfType<Spinner>())
            objects.Add(new Bananas(spinner.code.Split(','), beatmap, spinner));

        return objects;
    }

    /// <summary>Get all edge times except the slider head.</summary>
    private static IEnumerable<double> GetEdgeTimes(Slider sObject)
    {
        for (var i = 0; i < sObject.EdgeAmount; ++i)
            yield return sObject.time + sObject.GetCurveDuration() * (i + 1);
    }

    private static ICatchHitObject CreateJuiceStreamPart(Beatmap beatmap, ICatchHitObject sliderHead, Slider slider, double time, JuiceStream.JuiceStreamPart.PartKind kind)
    {
        var codeClone = slider.code.Split(',');
        codeClone[0] = slider.GetPathPosition(time).X.ToString(CultureInfo.InvariantCulture);
        codeClone[2] = time.ToString(CultureInfo.InvariantCulture);
        return new JuiceStream.JuiceStreamPart(codeClone, beatmap, slider, kind);
    }

    /// <summary>
    /// Calculates movement metadata (dash/hyper distances, direction, edge movement) between sequential objects.
    /// </summary>
    private static void CalculateDistances(List<ICatchHitObject> allObjects, Beatmap beatmap)
    {
        if (allObjects.Count < 2) return;
        allObjects.Sort((a, b) => a.Time.CompareTo(b.Time));

        var catcherWidth = CalculateCatchWidth(beatmap.DifficultySettings.circleSize);
        var halfCatcherWidth = catcherWidth * 0.5f / AllowedCatchRange;
        var baseWalkRange = halfCatcherWidth * BaseWalkRangeScale;

        var lastDirection = CatchNoteDirection.None;
        double dashRange = halfCatcherWidth;
        var walkRange = baseWalkRange;
        var lastWasHyper = false;

        for (var i = 0; i < allObjects.Count - 1; i++)
        {
            var current = allObjects[i];
            var next = allObjects[i + 1];
            current.Target = next;

            // Spinner gap logic now polymorphic
            if (current is Bananas || next is Bananas)
            {
                current.MovementType = CatchMovementType.Walk;
                dashRange = halfCatcherWidth;
                walkRange = baseWalkRange;
                lastDirection = CatchNoteDirection.None;
                lastWasHyper = false;
                continue;
            }

            var direction = next.Position.X > current.Position.X ? CatchNoteDirection.Left : CatchNoteDirection.Right;
            var timeToNext = next.Time - current.Time - QuarterFrameGrace;
            var distance = Math.Abs(next.Position.X - current.Position.X);

            var actualWalkRange = lastDirection == direction ? walkRange : baseWalkRange;
            if (lastWasHyper) actualWalkRange *= HyperdashLeniency;

            var dashDistanceToNext = distance - (lastDirection == direction ? dashRange : halfCatcherWidth);
            current.DistanceToHyper = (float)(timeToNext * BaseDashSpeed - dashDistanceToNext);

            var walkDistanceToNext = distance - actualWalkRange;
            current.DistanceToDash = (float)(timeToNext * BaseWalkSpeed - walkDistanceToNext);

            current.MovementType = current.DistanceToHyper < 0 ? CatchMovementType.Hyperdash : current.DistanceToDash < 0 ? CatchMovementType.Dash : CatchMovementType.Walk;
            current.TimeToTarget = timeToNext;

            dashRange = current.MovementType == CatchMovementType.Hyperdash ? halfCatcherWidth : Math.Clamp(current.DistanceToHyper, 0, halfCatcherWidth);
            walkRange = current.MovementType == CatchMovementType.Dash ? baseWalkRange : Math.Clamp(current.DistanceToDash, 0, baseWalkRange);

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

    private static double GetBpm(this Beatmap beatmap, ICatchHitObject obj)
    {
        var timingLine = beatmap.GetTimingLine(obj.Time);
        return GetBeatsPerMinute(timingLine);
    }

    private static double GetBpmScale(this Beatmap beatmap, ICatchHitObject obj) => 180 / beatmap.GetBpm(obj);

    private static bool IsEdgeMovement(Beatmap beatmap, ICatchHitObject obj)
    {
        switch (obj.MovementType)
        {
            case CatchMovementType.Walk:
                // No note after this so we can't determine edge movement.
                if (obj.Target == null)
                {
                    return false;
                }
                
                var comfyDash = 1.44 * beatmap.GetBpm(obj);
                var xDistance = Math.Abs(obj.Position.X - obj.Target.Position.X);
                return xDistance > comfyDash * beatmap.GetBpmScale(obj);
            case CatchMovementType.Dash:
                var pixelScale = 10.0 * beatmap.GetBpmScale(obj);
                return obj.DistanceToHyper < pixelScale;
            case CatchMovementType.Hyperdash:
            default:
                return false;
        }
    }
}