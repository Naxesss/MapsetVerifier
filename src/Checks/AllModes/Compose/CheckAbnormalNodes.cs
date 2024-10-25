using System;
using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Compose
{
    [Check]
    public class CheckAbnormalNodes : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Compose",
                Message = "Abnormal amount of slider nodes.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing mappers from writing inappropriate or otherwise harmful messages using slider nodes.
                    <image-right>
                        https://i.imgur.com/rlCoEtZ.png
                        An example of text being written with slider nodes in a way which can easily be hidden offscreen.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    The code of conduct applies to all aspects of the ranking process, including the beatmap content itself, 
                    whether that only be visible through the editor or in gameplay as well."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Abnormal",
                    new IssueTemplate(Issue.Level.Warning, "{0} Slider contains {1} nodes.", "timestamp - ", "amount").WithCause("A slider contains more nodes than 10 times the square root of its length in pixels.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
                if (hitObject is Slider slider && slider.NodePositions.Count > 10 * Math.Sqrt(slider.PixelLength))
                    yield return new Issue(GetTemplate("Abnormal"), beatmap, Timestamp.Get(slider), slider.NodePositions.Count);
        }
    }
}