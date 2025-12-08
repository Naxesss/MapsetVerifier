using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using static MapsetVerifier.Parser.Objects.HitObjects.Catch.JuiceStream.JuiceStreamPart.PartKind;

namespace MapsetVerifier.Checks.Catch.Compose.Rain
{
    [Check]
    public class CheckRainHyperdashSlider : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Contains hyperdashes on repeat or droplet.",
            Modes = new[] { Beatmap.Mode.Catch },
            Difficulties = new[] { Beatmap.Difficulty.Insane },
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Hyperdashes on slider droplets or repeats in Rains are discouraged."
                },
                {
                    "Reason",
                    @"
                    Players should be somewhat comfortable with hyperdashes on Rains, it is still discouraged to use any repeat or droplet hyperdashes maintain a manageable difficulty progression."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "SliderHyperRain",
                    new IssueTemplate(Issue.Level.Warning,
                            "{0} {1} hyperdashes should not be used.",
                            "timestamp -", "object")
                        .WithCause("Hyperdash is put on a droplet or slider repeat.")
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

                // Only consider JuiceStream parts contain droplet and repeats which can potentially have unrankable hyperdashes.
                if (current is not JuiceStream.JuiceStreamPart part) continue;
                
                if (part.Kind is Droplet or Repeat && part.MovementType == CatchMovementType.Hyperdash)
                {
                    // We don't have to create an issue for a Platter given that is already covered in CheckHasHyperdash.
                    yield return new Issue(
                        GetTemplate("SliderHyperRain"),
                        beatmap,
                        CatchExtensions.GetTimestamps(part, next),
                        part.GetNoteTypeName()
                    ).ForDifficulties(Beatmap.Difficulty.Insane);
                }
            }
        }
    }
}