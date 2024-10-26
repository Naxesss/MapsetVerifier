using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Spread
{
    [Check]
    public class CheckShortSliders : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = new[]
                {
                    Beatmap.Mode.Standard
                },
                Difficulties = new[]
                {
                    Beatmap.Difficulty.Easy
                },
                Category = "Spread",
                Message = "Too short sliders.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing slider head and tail from being too close in time for easy difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Newer players need time to comprehend when to hold down and let go of sliders. If a slider ends too quickly, 
                    the action of pressing the slider and very shortly afterwards letting it go will sometimes be difficult to 
                    handle. The action of lifting a key is similar in difficulty to pressing a key for newer players. So any 
                    distance in time you wouldn't place circles apart, you shouldn't place slider head and tail apart either."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Too Short",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1} ms, expected at least {2}.", "timestamp - ", "duration", "threshold").WithCause("A slider in an Easy difficulty is less than 125 ms (240 bpm 1/2).")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // Shortest length before warning is 1/2 at 240 BPM, 125 ms.
            const double timeThreshold = 125;

            foreach (var slider in beatmap.HitObjects.OfType<Slider>())
                if (slider.EndTime - slider.time < timeThreshold)
                    yield return new Issue(GetTemplate("Too Short"), beatmap, Timestamp.Get(slider), $"{slider.EndTime - slider.time:0.##}", timeThreshold);
        }
    }
}