using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
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

    public override CheckMetadata GetMetadata() =>
        new BeatmapCheckMetadata
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
                    Bear in mind that slider tails, repeats, drops and spinner bananas also count towards combo. 
                    A combo made of a single slider can't be split up, so it is only pointed out as a minor issue."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates()
    {
        return new Dictionary<string, IssueTemplate>
        {
            {
                "MaxComboSignificantly",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "{0} The amount of combo exceeds the guideline of {1} significantly, currently it has {2}.",
                    "timestamp -",
                    "guideline combo",
                    "combo"
                )
                    .WithCause("The combo amount exceeds the guideline significantly.")
                    .WithRule(
                        RC.Catch.CombosNotReachUnreasonableLengths,
                        RC.Catch.Cup_CombosNotExceed8Fruits,
                        RC.Catch.Salad_CombosNotExceed10Fruits,
                        RC.Catch.Platter_CombosNotExceed12Fruits
                    )
            },
            {
                "MaxCombo",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} The amount of combo exceeds the guideline of {1}, currently it has {2}.",
                    "timestamp -",
                    "guideline combo",
                    "combo"
                )
                    .WithCause("The combo amount exceeds the guideline.")
                    .WithRule(
                        RC.Catch.CombosNotReachUnreasonableLengths,
                        RC.Catch.Cup_CombosNotExceed8Fruits,
                        RC.Catch.Salad_CombosNotExceed10Fruits,
                        RC.Catch.Platter_CombosNotExceed12Fruits
                    )
            },
            {
                "MaxComboSingleSlider",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} The amount of combo exceeds the guideline of {1}, currently it has {2}. This combo is a single slider.",
                    "timestamp -",
                    "guideline combo",
                    "combo"
                )
                    .WithCause("The combo amount of a single slider exceeds the guideline.")
                    .WithRule(
                        RC.Catch.CombosNotReachUnreasonableLengths,
                        RC.Catch.Cup_CombosNotExceed8Fruits,
                        RC.Catch.Salad_CombosNotExceed10Fruits,
                        RC.Catch.Platter_CombosNotExceed12Fruits
                    )
            },
        };
    }

    private Issue ComboIssue(
        Beatmap beatmap,
        HitObject startObject,
        int maxCombo,
        int currentCount,
        Beatmap.Difficulty[] difficulties,
        string templateKey
    )
    {
        return new Issue(
            GetTemplate(templateKey),
            beatmap,
            Timestamp.Get(startObject),
            maxCombo,
            currentCount
        ).ForDifficulties(difficulties);
    }

    private Issue? GetComboIssues(
        Beatmap beatmap,
        HitObject startObject,
        int count,
        bool isSingleSlider,
        int threshold,
        params Beatmap.Difficulty[] difficulties
    )
    {
        if (count <= threshold)
        {
            return null;
        }

        // A single slider can't be split up by new combos, so only point it out.
        if (isSingleSlider)
        {
            return ComboIssue(
                beatmap,
                startObject,
                threshold,
                count,
                difficulties,
                "MaxComboSingleSlider"
            );
        }

        var templateKey =
            count > threshold * SignificantMultiplier ? "MaxComboSignificantly" : "MaxCombo";

        return ComboIssue(beatmap, startObject, threshold, count, difficulties, templateKey);
    }

    private IEnumerable<Issue?> GetComboIssues(
        Beatmap beatmap,
        HitObject startObject,
        int count,
        bool isSingleSlider
    )
    {
        yield return GetComboIssues(
            beatmap,
            startObject,
            count,
            isSingleSlider,
            ThresholdCup,
            Beatmap.Difficulty.Easy
        );
        yield return GetComboIssues(
            beatmap,
            startObject,
            count,
            isSingleSlider,
            ThresholdSalad,
            Beatmap.Difficulty.Normal
        );
        yield return GetComboIssues(
            beatmap,
            startObject,
            count,
            isSingleSlider,
            ThresholdPlatter,
            Beatmap.Difficulty.Hard
        );
        yield return GetComboIssues(
            beatmap,
            startObject,
            count,
            isSingleSlider,
            ThresholdRainAndOverdose,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
            Beatmap.Difficulty.Ultra
        );
    }

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        // Don't include sliders as on slider starts can be used to put new combos
        var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: false);

        if (catchObjects.Count == 0)
        {
            yield break;
        }

        var startObject = (HitObject)catchObjects[0];
        var count = 0;
        var objectsInCombo = 0;
        var issues = new List<Issue?>();

        for (var i = 0; i < catchObjects.Count; i++)
        {
            var catchObject = catchObjects[i];
            var currentObject = (HitObject)catchObject;
            // The first object always starts the first combo, so it only needs counting.
            if (
                i > 0
                && (currentObject.HasType(Types.NewCombo) || currentObject.HasType(Types.Spinner))
            )
            {
                issues.AddRange(
                    GetComboIssues(
                        beatmap,
                        startObject,
                        count,
                        IsSingleSlider(startObject, objectsInCombo)
                    )
                );

                // Reset values for new combo
                startObject = currentObject;
                count = 0;
                objectsInCombo = 0;
            }

            count++;
            objectsInCombo++;

            // Repeats, tails and drops all add to the combo
            if (catchObject is JuiceStream juiceStream)
            {
                count += juiceStream.Parts.Count;
            }
        }

        // Check last combo of the map
        issues.AddRange(
            GetComboIssues(beatmap, startObject, count, IsSingleSlider(startObject, objectsInCombo))
        );

        foreach (var issue in issues.OfType<Issue>())
        {
            yield return issue;
        }
    }

    private static bool IsSingleSlider(HitObject startObject, int objectsInCombo) =>
        objectsInCombo == 1 && startObject is JuiceStream;
}
