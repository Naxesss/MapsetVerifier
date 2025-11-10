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
                    </br>
                    And hyperdashes can't be used on drops and/or slider repetitions in Platters."
                },
                {
                    "Reasoning",
                    @"
                    This is to ensure an easy starting experience to beginner players in Cups.
                    </br>
                    This is to ensure a manageable step in difficulty for novice players in Salads.
                    </br>
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
                            "timestamp - ", "object")
                        .WithCause(
                            "Distance between the two objects is too high, triggering a hyperdash distance")
                },
                { "HyperdashSliderPart",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Slider hyperdash on {1}.",
                            "timestamp - ", "object")
                        .WithCause(
                            "Distance between the two objects is too high, triggering a hyperdash distance")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var catchObjects = beatmap.GetCatchHitObjects();

            foreach (var catchObject in catchObjects.Where(o => 
                         o is not JuiceStream &&
                         o.MovementType == CatchMovementType.Hyperdash))
            {
                yield return new Issue(
                    GetTemplate("Hyperdash"),
                    beatmap,
                    CatchExtensions.GetTimestamps(catchObject, catchObject.Target),
                    catchObject.GetNoteTypeName()
                ).ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal);
            }

            foreach (var juiceStream in catchObjects.Where(o => o is JuiceStream).Cast<JuiceStream>())
            {
                if (juiceStream.MovementType == CatchMovementType.Hyperdash)
                {
                    var firstSliderPart = juiceStream.Parts.First();

                    yield return new Issue(
                        GetTemplate("HyperdashSliderPart"),
                        beatmap,
                        CatchExtensions.GetTimestamps(juiceStream, firstSliderPart),
                        firstSliderPart.GetNoteTypeName().ToLower()
                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                }

                var sliderParts = juiceStream.Parts.Where(part => part.MovementType == CatchMovementType.Hyperdash);

                foreach (var sliderPart in sliderParts)
                {
                    // Hyperdashes are allowed on slider tails
                    if (sliderPart.Kind is JuiceStream.JuiceStreamPart.PartKind.Tail)
                    {
                        continue;
                    }
                    
                    yield return new Issue(
                        GetTemplate("HyperdashSliderPart"),
                        beatmap,
                        CatchExtensions.GetTimestamps(sliderPart, sliderPart.Target),
                        sliderPart.GetNoteTypeName().ToLower()
                    ).ForDifficulties(Beatmap.Difficulty.Hard);
                }
            }
        }
    }
}