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
    /// Creates CatchHitObjects from base HitObjects (slider parts, circles, spinners) and enriches them with movement data.
    /// </summary>
    public static List<ICatchHitObject> CreateCatchHitObjects(Beatmap beatmap, List<HitObject> originalHitObjects)
    {
        var hitObjects = GenerateCatchHitObjects(beatmap, originalHitObjects);
        CalculateDistances(hitObjects, beatmap);
        return hitObjects;
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
            
            var parts = new List<JuiceStream.JuiceStreamPart>();

            // Repeats & tail parts
            var edgeTimes = GetEdgeTimes(slider).ToList();
            for (var i = 0; i < edgeTimes.Count; i++)
            {
                var time = edgeTimes[i];
                var partKind = i + 1 == edgeTimes.Count ? JuiceStream.JuiceStreamPart.PartKind.Tail : JuiceStream.JuiceStreamPart.PartKind.Repeat;
                parts.Add(CreateJuiceStreamPart(beatmap, slider, time, partKind));
            }
            // Droplets
            foreach (var tickTime in slider.SliderTickTimes)
                parts.Add(CreateJuiceStreamPart(beatmap, slider, tickTime, JuiceStream.JuiceStreamPart.PartKind.Droplet));

            var sortedParts = parts.OrderBy(part => part.Time).ToList();
            juiceStream.Parts.AddRange(sortedParts);
            // We don't add the parts to the result objects as they all get referenced from the juice stream itself
        }

        // Circles -> Fruit
        foreach (var circle in originalHitObjects.OfType<Circle>())
            objects.Add(new Fruit(circle.code.Split(','), beatmap, circle));

        // Spinners -> Bananas
        foreach (var spinner in originalHitObjects.OfType<Spinner>())
            objects.Add(new Bananas(spinner.code.Split(','), beatmap, spinner));

        return objects.OrderBy(obj => obj.Time).ToList();
    }

    /// <summary>Get all edge times except the slider head.</summary>
    private static IEnumerable<double> GetEdgeTimes(Slider sObject)
    {
        for (var i = 0; i < sObject.EdgeAmount; ++i)
            yield return sObject.time + sObject.GetCurveDuration() * (i + 1);
    }

    private static JuiceStream.JuiceStreamPart CreateJuiceStreamPart(Beatmap beatmap, Slider slider, double time, JuiceStream.JuiceStreamPart.PartKind kind)
    {
        // Make sure we make a copy to not modify the original slider code
        var objectCodeCopy = (string) slider.code.Clone();
        var codeClone =objectCodeCopy.Split(',');
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