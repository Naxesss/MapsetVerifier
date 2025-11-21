using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;

namespace MapsetVerifier.Checks.Catch.Compose
{
    [Check]
    public class CheckConsecutiveHyperdash : BeatmapCheck
    {
        // Hyperdashes that are basic-snapped must not be used more than two times between consecutive fruits.
        private const int ThresholdPlatterBasic = 2;
        
        // Hyperdashes that are higher-snapped must not be used in conjunction with any other dashes or hyperdashes.
        private const int ThresholdPlatterHigher = 1;
        
        // Hyperdashes that are basic-snapped must not be used more than four times between consecutive fruits.
        private const int ThresholdRainBasic = 4;
        
        // Hyperdashes that are basic-snapped must not be used more than two times within a slider.
        private const int ThresholdRainBasicSlider = 2;
        
        // Hyperdashes that are higher-snapped must not be used in conjunction with any other hyperdashes.
        private const int ThresholdRainHigher = 1;

        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Category = "Compose",
            Message = "Too many consecutive hyperdashes.",
            Difficulties = [Beatmap.Difficulty.Hard, Beatmap.Difficulty.Insane],
            Modes = [Beatmap.Mode.Catch],
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    **Rain** : 
                    Hyperdashes that are basic-snapped must not be used more than four times between consecutive fruits.

                    **Platter** : 
                    Hyperdashes that are basic-snapped must not be used more than two times between consecutive fruits."
                },
                {
                    "Reasoning",
                    @"
                    The amount of hyperdashes used in a difficulty should be increasing which each difficulty level.
                    In platters the maximum amount of hyperdashes is set to two because the difficulty is meant to be an introduction to hypers."
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                { "BasicConsecutive",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Too many consecutive basic-snapped hyperdashes were used and should be at most {1}, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause("Too many consecutive basic-snapped hyperdash are used.")
                },
                { "HigherConsecutive",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Too many consecutive higher-snapped hyperdashes were used and should be at most {1}, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause("Too many consecutive higher-snapped hyperdash are used.")
                },
                { "RainBasicConsecutiveSlider",
                    new IssueTemplate(Issue.Level.Problem,
                            "{0} Too many consecutive basic-snapped hyperdashes were used within a slider and should be at most {1}, currently {2}.",
                            "timestamp - ", "rule amount", "amount")
                        .WithCause("Too many consecutive hyperdash are used in a Slider body.")
                }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var catchObjects = beatmap.GetCatchHitObjects(includeJuiceStreamParts: true);
            var trackedHyperdashObjects = new List<ICatchHitObject>();

            for (var i = 0; i < catchObjects.Count; i++)
            {
                var current = catchObjects[i];
                var next = i < catchObjects.Count - 1 ? catchObjects[i + 1] : null;
                
                if (current.MovementType == CatchMovementType.Hyperdash)
                {
                    trackedHyperdashObjects.Add(current);
                }
                else if (trackedHyperdashObjects.Count > 0)
                {
                    foreach (var issue in CheckTrackedHyperdashes(beatmap, next, trackedHyperdashObjects))
                        yield return issue;
                    
                    // Reset the tracked hypers after checking
                    trackedHyperdashObjects = [];
                }

                if (next == null && trackedHyperdashObjects.Count > 0)
                {
                    // We reached the end of the map, check the last tracked hypers
                    foreach (var issue in CheckTrackedHyperdashes(beatmap, next, trackedHyperdashObjects))
                        yield return issue;

                    trackedHyperdashObjects = [];
                }
            }
        }

        private IEnumerable<Issue> CheckTrackedHyperdashes(Beatmap beatmap, ICatchHitObject? last, List<ICatchHitObject> trackedHyperdashObjects)
        {
            // We reached the end of a chain of hyperdashes, check how many there were
            var count = trackedHyperdashObjects.Count;
            var objects = trackedHyperdashObjects.ToArray();

            // Slider tail hypers are fine in rains
            var isInSlider = trackedHyperdashObjects.Any(obj => 
                obj is JuiceStream ||
                (obj is JuiceStream.JuiceStreamPart juiceStreamPart && juiceStreamPart.Kind != JuiceStream.JuiceStreamPart.PartKind.Tail));

            var trackedPlusLast = trackedHyperdashObjects.ToList();
            if (last != null)
                trackedPlusLast.Add(last);
            
            if (IsAnyHigherSnapped(trackedPlusLast, Beatmap.Difficulty.Hard) && count > ThresholdPlatterHigher)
            {
                yield return new Issue(
                    GetTemplate("HigherConsecutive"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects),
                    ThresholdPlatterHigher,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }
            else if (count > ThresholdPlatterBasic)
            {
                yield return new Issue(
                    GetTemplate("BasicConsecutive"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects),
                    ThresholdPlatterBasic,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Hard);
            }

            if (IsAnyHigherSnapped(trackedPlusLast, Beatmap.Difficulty.Insane) && count > ThresholdRainHigher)
            {
                yield return new Issue(
                    GetTemplate("HigherConsecutive"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects),
                    ThresholdRainHigher,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Insane);
            }
            else if (isInSlider && count > ThresholdRainBasicSlider)
            {
                yield return new Issue(
                    GetTemplate("RainBasicConsecutiveSlider"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects),
                    ThresholdRainBasicSlider,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Insane);
            }
            else if (count > ThresholdRainBasic)
            {
                yield return new Issue(
                    GetTemplate("BasicConsecutive"),
                    beatmap,
                    CatchExtensions.GetTimestamps(objects),
                    ThresholdRainBasic,
                    count
                ).ForDifficulties(Beatmap.Difficulty.Insane);
            }
        }
    
        private static bool IsAnyHigherSnapped(List<ICatchHitObject> trackedPlusLast, Beatmap.Difficulty difficulty)
        {
            for (var i = 0; i < trackedPlusLast.Count - 1; i++)
            {
                var current = trackedPlusLast[i];
                var next = trackedPlusLast[i + 1];
                
                if (current.IsHigherSnapped(next, difficulty))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
