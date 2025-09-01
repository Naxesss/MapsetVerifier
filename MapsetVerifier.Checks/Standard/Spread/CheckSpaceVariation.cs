using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Spread
{
    [Check]
    public class CheckSpaceVariation : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard
                ],
                Difficulties =
                [
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal
                ],
                Category = "Spread",
                Message = "Object too close or far away from previous.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring spacing between objects with the same snapping is recognizable, and objects with different snappings are 
                    distinguishable, for easy and normal difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Time distance equality is a fundamental concept used in low difficulties to teach newer players how to interpret 
                    rhythm easier. By trivializing reading, these maps can better teach how base mechanics work, like approach circles, 
                    slider follow circles, object fading, hit bursts, hit sounds, etc.
                    <br \><br \>
                    Once these are learnt, and by the time players move on to hard difficulties, more advanced concepts and elements 
                    can begin to be introduced, like multiple reverses, spacing as a form of emphasis, complex rhythms, streams, and so 
                    on."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Distance",
                    new IssueTemplate(Issue.Level.Warning, "{0} Distance is {1} px, expected {2}, see {3}.", "timestamp - ", "distance", "distance", "example objects").WithCause("The distance between two hit objects noticeably contradicts a recent use of time distance balance between another " + "two hit objects using a similar time gap.")
                },

                {
                    "Ratio",
                    new IssueTemplate(Issue.Level.Warning, "{0} Distance/time ratio is {1}, expected {2}.", "timestamp - ", "ratio", "ratio").WithCause("The distance/time ratio between the previous hit objects greatly contradicts a following use of distance/time ratio.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            double deltaTime;

            var observedDistances = new List<ObservedDistance>();
            ObservedDistance? observedIssue = null;

            const double leniencyPercent = 0.15;
            const double leniencyAbsolute = 10;

            const double snapLeniencyPercent = 0.1;

            const double ratioLeniencyPercent = 0.2;
            const double ratioLeniencyAbsolute = 0.1;

            foreach (var hitObject in beatmap.HitObjects)
            {
                var nextObject = hitObject.Next();

                // Ignore spinners, since they have no clear start or end.
                if (hitObject is Spinner || nextObject is Spinner || nextObject == null)
                    continue;

                deltaTime = nextObject.GetPrevDeltaTime();

                // Ignore objects 2 beats or more apart (assuming 200 bpm), since they don't really hang together context-wise.
                if (deltaTime > 600)
                    continue;

                var distance = nextObject.GetPrevDistance();

                // Ignore stacks and half-stacks, since these are relatively normal.
                if (distance < 8)
                    continue;

                var closeDistanceSum = observedDistances.Sum(observedDistance => observedDistance.hitObject.time > hitObject.time - 4000 ? observedDistance.distance / observedDistance.deltaTime : 0);

                var closeDistanceCount = observedDistances.Count(observedDistance => observedDistance.hitObject.time > hitObject.time - 4000);

                var hasCloseDistances = closeDistanceCount > 0;
                var avrRatio = hasCloseDistances ? closeDistanceSum / closeDistanceCount : -1;

                // Checks whether a similar snapping has already been observed and uses that as
                // reference for determining if the current is too different.
                var index = observedDistances.FindLastIndex(observedDistance => deltaTime <= observedDistance.deltaTime * (1 + snapLeniencyPercent) && deltaTime >= observedDistance.deltaTime * (1 - snapLeniencyPercent) && observedDistance.hitObject.time > hitObject.time - 4000);

                if (index != -1)
                {
                    var distanceExpected = observedDistances[index].distance;

                    if ((Math.Abs(distanceExpected - distance) - leniencyAbsolute) / distance > leniencyPercent)
                    {
                        // Prevents issues from duplicating due to error being different compared to both before and after.
                        // (e.g. if 1 -> 2 is too large, and 2 -> 3 is only too small because of 1 -> 2 being an issue, we
                        // only mention 1 -> 2 rather than both, since they stem from the same issue)
                        var distanceExpectedAlternate = observedIssue?.distance ?? 0;

                        if (observedIssue != null && Math.Abs(distanceExpectedAlternate - distance) / distance <= leniencyPercent)
                        {
                            observedDistances[index] = new ObservedDistance(deltaTime, distance, hitObject);
                            observedIssue = null;
                        }
                        else
                        {
                            var prevObject = observedDistances[index].hitObject;
                            var prevNextObject = prevObject.Next();

                            yield return new Issue(GetTemplate("Distance"), beatmap, Timestamp.Get(hitObject, nextObject), (int)Math.Round(distance), (int)Math.Round(distanceExpected), Timestamp.Get(prevObject, prevNextObject));

                            observedIssue = new ObservedDistance(deltaTime, distance, hitObject);
                        }
                    }
                    else
                    {
                        observedDistances[index] = new ObservedDistance(deltaTime, distance, hitObject);
                        observedIssue = null;
                    }
                }
                else
                {
                    if (hasCloseDistances && (distance / deltaTime - ratioLeniencyAbsolute > avrRatio * (1 + ratioLeniencyPercent) || distance / deltaTime + ratioLeniencyAbsolute < avrRatio * (1 - ratioLeniencyPercent)))
                    {
                        var ratio = $"{distance / deltaTime:0.##}";
                        var ratioExpected = $"{avrRatio:0.##}";

                        yield return new Issue(GetTemplate("Ratio"), beatmap, Timestamp.Get(hitObject, nextObject), ratio, ratioExpected);
                    }
                    else
                    {
                        observedDistances.Add(new ObservedDistance(deltaTime, distance, hitObject));
                        observedIssue = null;
                    }
                }
            }
        }

        private readonly struct ObservedDistance
        {
            public readonly double deltaTime;
            public readonly double distance;
            public readonly HitObject hitObject;

            public ObservedDistance(double deltaTime, double distance, HitObject hitObject)
            {
                this.deltaTime = deltaTime;
                this.distance = distance;
                this.hitObject = hitObject;
            }
        }
    }
}