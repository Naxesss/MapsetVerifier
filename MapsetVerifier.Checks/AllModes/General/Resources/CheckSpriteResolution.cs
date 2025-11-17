using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckSpriteResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Too high sprite resolution.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing storyboard images from being extremely large."
                    },
                    {
                        "Reasoning",
                        @"
                    Unlike background images, storyboard images can be used to pan, zoom, scroll, rotate, etc, so they have more lenient 
                    limits in terms of resolution, but otherwise follow the same reasoning."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Resolution",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\"", "file name").WithCause("A storyboard image has a width height product exceeding 17,000,000 pixels.")
                },

                {
                    "Resolution Animation Frame",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" (Animation Frame)", "file name").WithCause("Same as the regular storyboard image check, except on one used in an animation.")
                },

                // parsing results
                {
                    "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" leaves the current song folder, which shouldn't ever happen.", "file name").WithCause("The file path of a storyboard image starts with two dots.")
                },

                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" is missing" + Common.CHECK_MANUALLY_MESSAGE, "file name").WithCause("A storyboard image referenced is not present.")
                },

                {
                    "Exception",
                    new IssueTemplate(Issue.Level.Error, Common.FILE_EXCEPTION_MESSAGE, "file name", "exception").WithCause("An exception occurred trying to parse a storyboard image.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // .osu
            foreach (var issue in Common.GetTagOsuIssues(beatmapSet, beatmap => beatmap.Sprites.Count > 0 ? beatmap.Sprites.Select(sprite => sprite.path) : [], GetTemplate, tagFile =>
                     {
                         // Executes for each non-faulty sprite file used in one of the beatmaps in the set.
                         var issues = new List<Issue>();

                         if (tagFile.File.Properties.PhotoWidth * tagFile.File.Properties.PhotoHeight > 17000000)
                             issues.Add(new Issue(GetTemplate("Resolution"), null, tagFile.TemplateArgs[0]));

                         return issues;
                     }))
                // Returns issues from both non-faulty and faulty files.
                yield return issue;

            foreach (var issue in Common.GetTagOsuIssues(beatmapSet, beatmap => beatmap.Animations.Count > 0 ? beatmap.Animations.SelectMany(animation => animation.framePaths) : [], GetTemplate, tagFile =>
                     {
                         var issues = new List<Issue>();

                         if (tagFile.File.Properties.PhotoWidth * tagFile.File.Properties.PhotoHeight > 17000000)
                             issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null, tagFile.TemplateArgs[0]));

                         return issues;
                     }))
                yield return issue;

            // .osb
            foreach (var issue in Common.GetTagOsbIssues(beatmapSet, osb => osb.sprites.Count > 0 ? osb.sprites.Select(sprite => sprite.path) : [], GetTemplate, tagFile =>
                     {
                         var issues = new List<Issue>();

                         if (tagFile.File.Properties.PhotoWidth * tagFile.File.Properties.PhotoHeight > 17000000)
                             issues.Add(new Issue(GetTemplate("Resolution"), null, tagFile.TemplateArgs[0]));

                         return issues;
                     }))
                yield return issue;

            foreach (var issue in Common.GetTagOsbIssues(beatmapSet, osb => osb.animations.Count > 0 ? osb.animations.SelectMany(animation => animation.framePaths) : [], GetTemplate, tagFile =>
                     {
                         var issues = new List<Issue>();

                         if (tagFile.File.Properties.PhotoWidth * tagFile.File.Properties.PhotoHeight > 17000000)
                             issues.Add(new Issue(GetTemplate("Resolution Animation Frame"), null, tagFile.TemplateArgs[0]));

                         return issues;
                     }))
                yield return issue;
        }
    }
}