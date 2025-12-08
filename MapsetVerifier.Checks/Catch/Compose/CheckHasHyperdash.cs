using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose
{
    [Check]
    public class CheckHasHyperdash : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Contains hyperdashes.",
            Modes = new[] { Beatmap.Mode.Catch },
            Difficulties = new [] { Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal, Beatmap.Difficulty.Hard },
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Hyperdashes are not allowed in Cups and Salads.

                    And hyperdashes can't be used on drops and/or slider repetitions in Platters."
                },
                {
                    "Reasoning",
                    @"
                    This is to ensure an easy starting experience to beginner players in Cups.

                    This is to ensure a manageable step in difficulty for novice players in Salads.

                    For Platters the accuracy and control required is unreasonable and can create a situation where the player potentially fails to read the slider path."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "Hyperdash",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} {1} is a hyperdash.",
                            "timestamp -", "object")
                        .WithCause(
                            "Distance between the two objects is too high, triggering a hyperdash distance")
                },
                { "HyperdashSliderPart",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Slider hyperdash on {1}.",
                            "timestamp -", "object")
                        .WithCause(
                            "Distance between the two objects is too high, triggering a hyperdash distance")
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

                if (current.MovementType != CatchMovementType.Hyperdash)
                {
                    continue;
                }

                // Report all hyperdashes in lower difficulties
                yield return new Issue(
                    GetTemplate("Hyperdash"),
                    beatmap,
                    CatchExtensions.GetTimestamps(current, next),
                    current.GetNoteTypeName()
                ).ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal);

                // Only report sliders in Platters
                if (current is JuiceStream)
                {
                    yield return new Issue(
                        GetTemplate("HyperdashSliderPart"),
                        beatmap,
                        CatchExtensions.GetTimestamps(current, next),
                        current.GetNoteTypeName().ToLower()
                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                }
                else if (current is JuiceStream.JuiceStreamPart part)
                {
                    // Hyperdashes are allowed on slider tails
                    if (part.Kind is JuiceStream.JuiceStreamPart.PartKind.Tail)
                    {
                        continue;
                    }
                    
                    yield return new Issue(
                        GetTemplate("HyperdashSliderPart"),
                        beatmap,
                        CatchExtensions.GetTimestamps(current, next),
                        current.GetNoteTypeName().ToLower()
                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                }
            }
        }
    }
}