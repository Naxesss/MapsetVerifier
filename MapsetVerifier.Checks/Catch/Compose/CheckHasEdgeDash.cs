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
                </br>
                On Rains edge dashes may only be used singularly. 
                </br>
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

    private static Issue EdgeDashIssue(IssueTemplate template, Beatmap beatmap, ICatchHitObject currentObject, params Beatmap.Difficulty[] difficulties)
    {
        var pixelDistance = (int) Math.Ceiling(currentObject.DistanceToHyper);
        var pixelDistanceMessage = pixelDistance == 1
            ? "1 pixel"
            : $"{pixelDistance} pixels";
        
        return new Issue(
            template,
            beatmap,
            CatchExtensions.GetTimestamps(currentObject, currentObject.Target),
            currentObject.GetNoteTypeName(),
            pixelDistanceMessage
        ).ForDifficulties(difficulties);
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);

        foreach (var nonHyperObject in catchObjects.Where(obj => obj.MovementType != CatchMovementType.Hyperdash))
        {
            if (float.IsPositiveInfinity(nonHyperObject.DistanceToHyper))
            {
                continue;
            }
            
            var bpmScale = beatmap.GetScaledBpm(nonHyperObject);
            // 5 pixels is quite far on a 180 BPM hyperdash, most likely not intended as an edge dash.
            var edgeDashDistance = bpmScale * 5;
            var pixelsUntilHyper = Math.Ceiling(nonHyperObject.DistanceToHyper);
            
            if (pixelsUntilHyper <= edgeDashDistance)
            {
                yield return EdgeDashIssue(GetTemplate("EdgeDash"), beatmap, nonHyperObject,
                    Beatmap.Difficulty.Insane);

                yield return EdgeDashIssue(GetTemplate("EdgeDashMinor"), beatmap, nonHyperObject,
                    Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra);
                
                yield return EdgeDashIssue(GetTemplate("EdgeDashProblem"), beatmap, nonHyperObject,
                    Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard);
            }
            else
            {
                var strongDashDistance = bpmScale * 20;

                if (pixelsUntilHyper <= strongDashDistance)
                {
                    yield return EdgeDashIssue(GetTemplate("StrongDash"), beatmap, nonHyperObject,
                        Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane,
                        Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra);
                }
            }
        }
    }
}