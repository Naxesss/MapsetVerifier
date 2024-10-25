using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.Events;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckOverlayLayer : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Resources",
                Message = "Overlay layer usage.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing storyboard elements from blocking the view of objects to the point where they become unnecessarily 
                    difficult, or even impossible, to read.
                    <image-right>
                        https://i.imgur.com/rVZpeso.png
                        A storyboard element appearing over a slider.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    This layer appears over the object layer, meaning it can obscure objects. This is potentially very dangerous if not paid 
                    attention to, as it can screw over the experience of the map really badly if executed poorly.
                    <br><br>
                    As a rough guideline, ensure that the position of aimed objects are clear and that indicators are identifiable (so for 
                    example when to start spinning or that a slider reverses). Basically don't cover up important gameplay elements unless 
                    otherwise clear."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" Check the {1} to see where it appears.", "file name", ".osu/.osb").WithCause("A storyboard sprite or animation is using the overlay layer.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Checks .osu-specific storyboard elements.
            foreach (var beatmap in beatmapSet.Beatmaps)
                foreach (var sprite in beatmap.Sprites)
                    if (sprite.layer == Sprite.Layer.Overlay)
                        yield return new Issue(GetTemplate("Warning"), beatmap, sprite.path, ".osu");

            foreach (var beatmap in beatmapSet.Beatmaps)
                foreach (var animation in beatmap.Animations)
                    if (animation.layer == Sprite.Layer.Overlay)
                        yield return new Issue(GetTemplate("Warning"), beatmap, animation.path, ".osu");

            // Checks .osb storyboard elements.
            if (beatmapSet.Osb == null)
                yield break;

            foreach (var sprite in beatmapSet.Osb.sprites)
                if (sprite.layer == Sprite.Layer.Overlay)
                    yield return new Issue(GetTemplate("Warning"), null, sprite.path, ".osb");

            foreach (var animation in beatmapSet.Osb.animations)
                if (animation.layer == Sprite.Layer.Overlay)
                    yield return new Issue(GetTemplate("Warning"), null, animation.path, ".osb");
        }
    }
}