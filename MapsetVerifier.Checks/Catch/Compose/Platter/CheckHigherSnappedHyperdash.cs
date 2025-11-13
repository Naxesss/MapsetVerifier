using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose.Platter;

[Check]
public class CheckHigherSnappedHyperdash : BeatmapCheck
{
    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Higher-snapped hyperdash.",
        Modes = [Beatmap.Mode.Catch],
        Difficulties = [Beatmap.Difficulty.Hard],
        Author = "Greaper",

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                Higher-snapped hyperdashes should not be followed by antiflow patterns."
            },
            {
                "Reason",
                @"
                When a higher-snapped hyperdash is followed by an antiflow pattern (a walk in the opposite direction), it can create a harsh experience for players. Given that Platters introduce hyperdashes they should be used thoughtfully."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            { "HigherSnapFollowedByAntiFlow",
                new IssueTemplate(Issue.Level.Warning,
                        "{0} Higher-snapped hyperdashes followed by antiflow.",
                        "timestamp - ")
            }
        };
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (var i = 0; i < catchObjects.Count; i++)
        {
            var current = catchObjects[i];
            var next = i < catchObjects.Count - 1 ? catchObjects[i + 1] : null;
            
            if (next == null) continue;

            // Only higher-snapped hyperdashes are relevant for this check.
            if (current.MovementType != CatchMovementType.Hyperdash || !current.IsHigherSnapped(next, Beatmap.Difficulty.Hard)) continue;
            
            var followedByWalk = next.MovementType == CatchMovementType.Walk;

            // No need to check for dashes or hyperdashes as they are covered in other checks.
            if (followedByWalk)
            {
                // Only direction changes are classified as antiflow patterns.
                if (current.NoteDirection == CatchNoteDirection.None || current.NoteDirection == next.NoteDirection)
                {
                    continue;
                }
                        
                // Hyperdashes that are higher-snapped should not be followed by antiflow patterns.
                yield return new Issue(
                    GetTemplate("HigherSnapFollowedByAntiFlow"),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, next)
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }
        }
    }
}