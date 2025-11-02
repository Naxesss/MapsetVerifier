using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Compose
{
    [Check]
    public class CheckConcurrent : BeatmapCheck
    {
        private const int ManiaConcurrentThresholdMs = 30;
        private const int OtherModesConcurrentThresholdMs = 10;
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

                    // Only need to check forwards, as any previous object will already have looked behind this one
                    var msApart = otherHitObject.time - hitObject.GetEndTime();
                    
                    // No need to check further if the next object is far apart from the current hit object
                    if (msApart > 30) break;
                    
                    if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                    {
                        var keys = (int) beatmap.DifficultySettings.circleSize;
                        
                        // In mania hit objects can be concurrent so we only need to check if they are in the same column
                        var hitObjectColumn = GetManiaColumn(hitObject, keys);
                        var otherHitObjectColumn = GetManiaColumn(otherHitObject, keys);
                        
                        if (hitObjectColumn != otherHitObjectColumn)
                        {
                            // Skip the object if they are not in the same column
                            continue;
                        }
                    }
                    
                    var timestamp = Timestamp.Get(hitObject);
                    var otherTimestamp = Timestamp.Get(otherHitObject);

                    if (msApart <= 0)
                        yield return new Issue(GetTemplate("Concurrent Objects"), beatmap, timestamp, otherTimestamp, ObjectsAsString(hitObject, otherHitObject));
                    
                    // Mania has only 1 input per column, so we need a bigger gap.
                    else if ((beatmap.GeneralSettings.mode == Beatmap.Mode.Mania && msApart <= ManiaConcurrentThresholdMs) ||
                             // Spinners can be 1 ms or further apart from the previous end time, but not at the same millisecond.
                             (msApart <= OtherModesConcurrentThresholdMs && otherHitObject is not Spinner))
                        yield return new Issue(GetTemplate("Almost Concurrent Objects"), beatmap, timestamp,
                            otherTimestamp, msApart);
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

        private static int GetManiaColumn(HitObject hitObject, float keys)
        {
            // Mania is rather weird as the X position isn't given in columns but rather pixels
            // Manual changes or certain editors can cause objects to be slightly off from their intended column
            return (int)hitObject.Position.X / (512 / (int)keys);
        }
    }
}