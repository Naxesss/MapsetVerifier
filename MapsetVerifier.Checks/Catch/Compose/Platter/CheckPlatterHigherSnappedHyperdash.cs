using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose.Platter;

[Check]
public class CheckPlatterHigherSnappedHyperdash : BeatmapCheck
{
    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
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
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            {
                "HigherSnapFollowedByAntiFlow",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} Higher-snapped hyperdashes on {1} followed by antiflow.",
                    "timestamp -",
                    "note"
                )
            },
        };
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        for (int i = 0; i < catchObjects.Count - 2; i++)
        {
            var current = catchObjects[i];
            var next = catchObjects[i + 1];
            var followUp = catchObjects[i + 2];

            // Only higher-snapped hyperdashes are relevant for this check.
            if (
                current.MovementType != CatchMovementType.Hyperdash
                || !current.IsHigherSnapped(next, Beatmap.Difficulty.Hard)
            )
                continue;

            // No need to check for dashes or hyperdashes as they are covered in other checks.
            if (next.MovementType == CatchMovementType.Walk)
            {
                // Only direction changes are classified as antiflow patterns.
                if (
                    next.NoteDirection == CatchNoteDirection.None
                    || current.NoteDirection == next.NoteDirection
                )
                {
                    continue;
                }

                // Additional check based on BPM scaling if we moved a lot of x distance
                // With 180 BPM allow 10 pixels of room
                var distanceToFollowUp = (int)Math.Abs(next.Position.X - followUp.Position.X);
                var timeBetweenMs = (int)(followUp.Time - next.Time);
                var timeBasedFactor = timeBetweenMs / 50.0;
                var allowedAntiflowDistance = (int)Math.Floor(10 * timeBasedFactor);

                if (distanceToFollowUp <= allowedAntiflowDistance)
                {
                    continue;
                }

                // Hyperdashes that are higher-snapped should not be followed by antiflow patterns.
                yield return new Issue(
                    GetTemplate("HigherSnapFollowedByAntiFlow"),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, next, followUp),
                    current.GetNoteTypeName()
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }
        }
    }
}
