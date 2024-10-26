using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.Compose
{
    [Check]
    public class CheckConcurrent : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Compose",
                Message = "Concurrent hit objects.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that only one object needs to be interacted with at any given moment in time."
                    },
                    {
                        "Reasoning",
                        @"
                    A clickable object during the duration of an already clicked object, for example a slider, is possible to play 
                    assuming the clickable object is within the slider circle whenever a slider tick/edge happens. However, there is 
                    no way for a player to intuitively know how to play such patterns as there is no tutorial for them, and they are 
                    not self-explanatory.
                    <br \><br \>
                    Sliders, spinners, and other holdable objects, teach the player to hold down the key for 
                    the whole duration of the object, so suddenly forcing them to press again would be contradictory to that 
                    fundamental understanding. Because of this, these patterns more often than not cause confusion, even where 
                    otherwise introduced well.
                    <image-right>
                        https://i.imgur.com/2bTX4aQ.png
                        A slider with two concurrent circles. Can be hit without breaking combo.
                    </image>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Concurrent Objects",
                    new IssueTemplate(Issue.Level.Problem, "{0} Concurrent {1}.", "timestamp - ", "hit objects").WithCause("A hit object starts before another hit object has ended. For mania this also " + "requires that the objects are in the same column.")
                },

                {
                    "Almost Concurrent Objects",
                    new IssueTemplate(Issue.Level.Problem, "{0} Within {1} ms of one another.", "timestamp - ", "gap").WithCause("Two hit objects are less than 10 ms apart from one another. For mania this also " + "requires that the objects are in the same column.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var hitObjectCount = beatmap.HitObjects.Count;

            for (var i = 0; i < hitObjectCount - 1; ++i)
                for (var j = i + 1; j < hitObjectCount; ++j)
                {
                    var hitObject = beatmap.HitObjects[i];
                    var otherHitObject = beatmap.HitObjects[j];

                    if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania && hitObject.Position.X.AlmostEqual(otherHitObject.Position.X))
                        continue;

                    // Only need to check forwards, as any previous object will already have looked behind this one.
                    var msApart = otherHitObject.time - hitObject.GetEndTime();

                    if (msApart <= 0)
                        yield return new Issue(GetTemplate("Concurrent Objects"), beatmap, Timestamp.Get(hitObject, otherHitObject), ObjectsAsString(hitObject, otherHitObject));

                    // Spinners can be 1 ms or further apart from the previous end time, but not at the same milisecond.
                    else if (msApart <= 10 && !(otherHitObject is Spinner))
                        yield return new Issue(GetTemplate("Almost Concurrent Objects"), beatmap, Timestamp.Get(hitObject, otherHitObject), msApart);

                    else
                        // Hit objects are sorted by time, meaning if the next one is > 10 ms away, any remaining will be too.
                        break;
                }
        }

        private static string ObjectsAsString(HitObject hitObject, HitObject otherHitObject)
        {
            var type = hitObject.GetObjectType();
            var otherType = otherHitObject.GetObjectType();

            return type == otherType ? type + "s" : type + " and " + otherType;
        }
    }
}