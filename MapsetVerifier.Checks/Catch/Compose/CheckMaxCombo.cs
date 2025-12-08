using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Parser.Objects.HitObject;

namespace MapsetVerifier.Checks.Catch.Compose;

[Check]
public class CheckMaxCombo : BeatmapCheck
{
    private const int ThresholdCup = 8;
    private const int ThresholdSalad = 10;
    private const int ThresholdPlatter = 12;
    private const int ThresholdRainAndOverdose = 16;

    private const double SignificantMultiplier = 1.5;

    public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
    {
        Category = "Compose",
        Message = "Too high combo.",
        Modes = [Beatmap.Mode.Catch],
        Author = "Greaper",

        Documentation = new Dictionary<string, string>
        {
            {
                "Purpose",
                @"
                    Combos should not reach unreasonable lengths."
            },
            {
                "Reasoning",
                @"
                    Caught fruits will stack up on the plate and can potentially obstruct the player's view. 
                    Bear in mind that slider tails, repeats and spinner bananas also count as fruits."
            }
        }
    };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            { "MaxComboSignificantly",
                new IssueTemplate(Issue.Level.Warning,
                        "{0} The amount of combo exceeds the guideline of {1} significantly, currently it has {2}.",
                        "timestamp -", "guideline combo", "combo")
                    .WithCause("The combo amount exceeds the guideline significantly.")
            },
            { "MaxCombo",
                new IssueTemplate(Issue.Level.Minor,
                        "{0} The amount of combo exceeds the guideline of {1}, currently it has {2}.",
                        "timestamp -", "guideline combo", "combo")
                    .WithCause("The combo amount exceeds the guideline.")
            }
        };
    }

    private Issue ComboIssue(Beatmap beatmap, HitObject startObject, int maxCombo, int currentCount,
        Beatmap.Difficulty[] difficulties, bool isSignificant = false)
    {
        return new Issue(
            GetTemplate(isSignificant ? "MaxComboSignificantly" : "MaxCombo"),
            beatmap,
            Timestamp.Get(startObject),
            maxCombo,
            currentCount
        ).ForDifficulties(difficulties);
    }

    private Issue? GetComboIssues(Beatmap beatmap, HitObject startObject, int count, int threshold, params Beatmap.Difficulty[] difficulties)
    {
        if (count > threshold * SignificantMultiplier)
        {
            return ComboIssue(beatmap, startObject, threshold, count, difficulties, true);
        }

        if (count > threshold)
        {
            return ComboIssue(beatmap, startObject, threshold, count, difficulties);
        }

        return null;
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var count = 0;
        // Don't include sliders as on slider starts can be used to put new combos
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: false);

        if (catchObjects.Count == 0)
        {
            yield break;
        }

        var startObject = (HitObject) catchObjects[0];
        var issues = new List<Issue?>();

        for (var i = 1; i < catchObjects.Count; i++)
        {
            var catchObject = catchObjects[i];
            var currentObject = (HitObject) catchObject;
            if (currentObject.HasType(Types.NewCombo) || currentObject.HasType(Types.Spinner))
            {
                issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdCup, Beatmap.Difficulty.Easy));
                issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdSalad, Beatmap.Difficulty.Normal));
                issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdPlatter, Beatmap.Difficulty.Hard));
                issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdRainAndOverdose, Beatmap.Difficulty.Insane,
                    Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra));

                // Reset values for new combo
                startObject = currentObject;
                count = 0;
            }
            
            count++;

            // Also count all juice stream parts as slider parts contribute to combo
            if (catchObject is JuiceStream juiceStream)
            {
                count += juiceStream.Parts.Count;
            }
        }

        // Check last combo of the map
        issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdCup, Beatmap.Difficulty.Easy));
        issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdSalad, Beatmap.Difficulty.Normal));
        issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdPlatter, Beatmap.Difficulty.Hard));
        issues.Add(GetComboIssues(beatmap, startObject, count, ThresholdRainAndOverdose, Beatmap.Difficulty.Insane, Beatmap.Difficulty.Expert, Beatmap.Difficulty.Ultra));

        foreach (var issue in issues.OfType<Issue>())
        {
            yield return issue;
        }
    }
}