using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose.Rain;

[Check]
public class CheckRainConsistentHyperdashSnaps : BeatmapCheck
{
    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Consecutive basic-snapped hyperdashes.",
        Modes = [Beatmap.Mode.Catch],
        Difficulties = [Beatmap.Difficulty.Insane],
        Author = "Greaper",

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                Mixing different snapped hyperdashes can be somewhat challenging, to keep Rain difficulties easy to read it is discouraged to mix different snapped hyperdashes."
            },
            {
                "Reason",
                @"
                Mixing different snapped hyperdashes is to be introduced at Overdoses to have a gradual difficulty increase."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            { "DifferentSnap",
                new IssueTemplate(Issue.Level.Warning,
                        "{0} Basic-snapped hyperdashes of different snap are used.",
                        "timestamp -")
                    .WithCause("Different snapped basic-snapped hyperdashes.")
            }
        };
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);
        var trackedObjects = new List<ICatchHitObject>();

        for (var i = 0; i < catchObjects.Count - 1; i++)
        {
            var current = catchObjects[i];
            var next = catchObjects[i + 1];

            if (current.MovementType == CatchMovementType.Hyperdash &&
                !current.IsHigherSnapped(next, Beatmap.Difficulty.Insane))
            {
                trackedObjects.Add(current);
            }
            else
            {
                // We got a non-hyperdash, or it was not basic-snapped, clear tracked objects
                trackedObjects.Clear();
            }

            // Hyperdash chain ended, evaluate tracked objects
            if (next.MovementType != CatchMovementType.Hyperdash)
            {
                // Need at least 2 tracked objects to compare snaps
                if (trackedObjects.Count < 2)
                {
                    trackedObjects.Clear();
                    continue;
                }
                
                // Add the last object as this is the target of the last hyperdash, which we need to compare snaps
                trackedObjects.Add(next);

                for (var j = 0; j < trackedObjects.Count - 2; j++)
                {
                    var first = trackedObjects[j];
                    var second = trackedObjects[j + 1];
                    var third = trackedObjects[j + 2];

                    if (CatchExtensions.IsSameSnap(first, second, third))
                    {
                        // The distance between the objects are the same snap
                        continue;
                    }
                    
                    // Hyperdashes that are basic-snapped should not be used consecutively when different beat snaps are used
                    yield return new Issue(
                        GetTemplate("DifferentSnap"),
                        beatmap,
                        CatchExtensions.GetTimestamps(trackedObjects.ToArray())
                    ).ForDifficulties(Beatmap.Difficulty.Insane);
                }
                
                // Clear tracked objects for next chain
                trackedObjects.Clear();
            }
        }
    }
}