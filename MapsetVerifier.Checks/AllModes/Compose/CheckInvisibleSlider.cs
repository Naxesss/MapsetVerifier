using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Compose
{
    [Check]
    public class CheckInvisibleSlider : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Compose",
                Message = "Invisible sliders.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing objects from being invisible.
                        ![](https://i.imgur.com/xJIwdbA.png)
                        A slider with no nodes; looks like a circle on the timeline but is invisible on the playfield."
                    },
                    {
                        "Reasoning",
                        @"
                        Although often used in combination with a storyboard to make up for the invisibility through sprites, there 
                        is no way to force the storyboard to appear, meaning players may play the map unaware that they should have 
                        enabled something for a fair gameplay experience."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Zero Nodes",
                    new IssueTemplate(Issue.Level.Problem, "{0} has no slider nodes.", "timestamp -").WithCause("A slider has no nodes.")
                },

                {
                    "Negative Length",
                    new IssueTemplate(Issue.Level.Problem, "{0} has negative pixel length.", "timestamp -").WithCause("A slider has a negative pixel length.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var slider in beatmap.HitObjects.OfType<Slider>())
                if (slider.NodePositions.Count == 0)
                    yield return new Issue(GetTemplate("Zero Nodes"), beatmap, Timestamp.Get(slider));
                else if (slider.PixelLength < 0)
                    yield return new Issue(GetTemplate("Negative Length"), beatmap, Timestamp.Get(slider));
        }
    }
}