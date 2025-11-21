using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckBgResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Too high or low background resolution.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing background quality from being noticeably low or unnoticeable high to save on file size.

                        ![](https://i.imgur.com/VrKRzse.png)
                        The left side is ~2.25x the resolution of the right side, which is the equivalent of comparing 
                        2560 x 1440 to 1024 x 640."
                    },
                    {
                        "Reasoning",
                        @"
                        Anything less than 1024 x 640 is usually quite noticeable, whereas anything higher than 2560 x 1440 
                        is unlikely to be visible with the setup of the average player.

                        > This uses 16:10 as base, since anything outside of 16:9 will be cut off on that aspect ratio rather than resized to fit the screen, preserving quality."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Too high",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" greater than 2560 x 1440 ({1} x {2})", "file name", "width", "height")
                        .WithCause("A background file has a width exceeding 2560 pixels or a height exceeding 1440 pixels.")
                },

                {
                    "Very low",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" lower than 1024 x 640 ({1} x {2})", "file name", "width", "height")
                        .WithCause("A background file has a width lower than 1024 pixels or a height lower than 640 pixels.")
                },

                {
                    "File size",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" has a file size exceeding 2.5 MB ({1} MB)", "file name", "file size")
                        .WithCause("A background file has a file size greater than 2.5 MB.")
                },

                // parsing results
                {
                    "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" leaves the current song folder, which shouldn't ever happen.", "file name")
                        .WithCause("The file path of a background file starts with two dots.")
                },

                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" is missing" + Common.CHECK_MANUALLY_MESSAGE, "file name")
                        .WithCause("A background file referenced is not present.")
                },

                {
                    "Exception",
                    new IssueTemplate(Issue.Level.Error, Common.FILE_EXCEPTION_MESSAGE, "file name", "exception info")
                        .WithCause("An exception occurred trying to parse a background file.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var issue in Common.GetTagOsuIssues(beatmapSet, beatmap => beatmap.Backgrounds.Count > 0 ? beatmap.Backgrounds.Select(bg => bg.path) : [], GetTemplate, tagFile =>
                 {
                     // Executes for each non-faulty background file used in one of the beatmaps in the set.
                     var issues = new List<Issue>();

                     if (tagFile.File.Properties.PhotoWidth > 2560 || tagFile.File.Properties.PhotoHeight > 1440)
                         issues.Add(new Issue(GetTemplate("Too high"), null, tagFile.TemplateArgs[0], tagFile.File.Properties.PhotoWidth, tagFile.File.Properties.PhotoHeight));

                     else if (tagFile.File.Properties.PhotoWidth < 1024 || tagFile.File.Properties.PhotoHeight < 640)
                         issues.Add(new Issue(GetTemplate("Very low"), null, tagFile.TemplateArgs[0], tagFile.File.Properties.PhotoWidth, tagFile.File.Properties.PhotoHeight));

                     // Most operating systems define 1 KB as 1024 B and 1 MB as 1024 KB,
                     // not 10^(3x) which the prefixes usually mean, but 2^(10x), since binary is more efficient for circuits,
                     // so since this is what your computer uses we'll use this too.
                     var megaBytes = new FileInfo(tagFile.File.Name).Length / Math.Pow(1024, 2);

                     if (megaBytes > 2.5)
                         issues.Add(new Issue(GetTemplate("File size"), null, tagFile.TemplateArgs[0], FormattableString.Invariant($"{megaBytes:0.##}")));

                     return issues;
                 }))
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
        }
    }
}