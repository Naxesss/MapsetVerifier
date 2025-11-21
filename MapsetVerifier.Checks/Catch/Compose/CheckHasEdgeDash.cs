using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckHasEdgeDash : BeatmapCheck
{
    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Edge dashes.",
        Modes = [Beatmap.Mode.Catch],
        Author = "Greaper",
        Difficulties =
        [
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
            Beatmap.Difficulty.Ultra
        ],

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                Edge dashes are hard and shouldn't be used on lower difficulties. 
                They can be used on Rains and Overdoses, although if they are used they mush be used properly."
            },
            {
                "Reasoning",
                @"
                Edge dashes require precise movement, on lower difficulties we cannot expect such accuracy from players.

                On Rains edge dashes may only be used singularly. 

                On Overdose they may be used with caution for a maximum of three consecutive objects, and should not be used after hyperdashes."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            { "StrongDash",
                new IssueTemplate(Issue.Level.Minor,
                        "{0} {1} is {2} away from becoming a hyper.",
                        "timestamp - ", "object", "x")
                    .WithCause(
                        "X amount of pixels off to become a hyperdash.")
            },
            { "EdgeDashMinor",
                new IssueTemplate(Issue.Level.Minor,
                        "{0} {1} is {2} away from becoming a hyper.",
                        "timestamp - ", "object", "x")
                    .WithCause(
                        "X amount of pixels off to become a hyperdash.")
            },
            { "EdgeDash",
                new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} is {2} away from becoming a hyper.",
                        "timestamp - ", "object", "x")
                    .WithCause(
                        "X amount of pixels off to become a hyperdash.")
            },
            { "EdgeDashProblem",
                new IssueTemplate(Issue.Level.Problem,
                        "{0} {1} is {2} away from becoming a hyper.",
                        "timestamp - ", "object", "x")
                    .WithCause(
                        "Usage of edge dashes on lower diffs.")
            }
        };
    }

    private static Issue EdgeDashIssue(IssueTemplate template, Beatmap beatmap, ICatchHitObject current, ICatchHitObject next, params Beatmap.Difficulty[] difficulties)
    {
        var pixelDistance = (int) Math.Ceiling(current.DistanceToHyper);
        var pixelDistanceMessage = pixelDistance == 1
            ? "1 pixel"
            : $"{pixelDistance} pixels";
        
        return new Issue(
            template,
            beatmap,
            CatchExtensions.GetTimestamps(current, next),
            current.GetNoteTypeName(),
            pixelDistanceMessage
        ).ForDifficulties(difficulties);
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (var i = 0; i < catchObjects.Count; i++)
        {
            var current = catchObjects[i];
            var next = i < catchObjects.Count - 1 ? catchObjects[i + 1] : null;

            // We are only interested in dashes
            if (current.MovementType == CatchMovementType.Hyperdash)
            {
                continue;
            }
            
            // Objects that can't have a hyperdash are ignored
            if (float.IsPositiveInfinity(current.DistanceToHyper))
            {
                continue;
            }
            
            var bpmScale = beatmap.GetScaledBpm(current);
            var pixelsUntilHyper = Math.Ceiling(current.DistanceToHyper);
            
            var timeToNext = next.Time - current.Time;

            var edgeDashDistance = bpmScale * GetCurvedDistance(ms: timeToNext, maxDistance: 10f);
            
            if (pixelsUntilHyper <= edgeDashDistance)
            {
                yield return EdgeDashIssue(GetTemplate("EdgeDash"), beatmap, current, next,
                    Beatmap.Difficulty.Insane);

                yield return EdgeDashIssue(GetTemplate("EdgeDashMinor"), beatmap, current, next,
                    Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra);
                
                yield return EdgeDashIssue(GetTemplate("EdgeDashProblem"), beatmap, current, next,
                    Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard);
            }
            else
            {
                var strongDashDistance = bpmScale * GetCurvedDistance(ms: timeToNext, maxDistance: 50f);

                if (pixelsUntilHyper <= strongDashDistance)
                {
                    yield return EdgeDashIssue(GetTemplate("StrongDash"), beatmap, current, next,
                        Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane,
                        Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra);
                }
            }
        }
    }
    
    /// <summary>
    /// At 300ms, returns the max distance in pixels
    /// At 180ms returns around half the pixels
    /// At 0ms, returns 0 pixels
    /// Applies a curve to make shorter time between objects result in less distance
    /// </summary>
    /// <param name="ms">The time between the two objects we want to get the curved distance for</param>
    /// <param name="maxDistance">The maximum distance we want to return</param>
    /// <returns></returns>
    private static float GetCurvedDistance(double ms, float maxDistance)
    {
        // Clamp x to 0–300 range
        var x = MathF.Max(0f, MathF.Min((float) ms, 300f));
        
        // Apply curve
        return (int) Math.Round(maxDistance * (1f - MathF.Pow(1f - (x / 300f), 0.75f)));
    }

}