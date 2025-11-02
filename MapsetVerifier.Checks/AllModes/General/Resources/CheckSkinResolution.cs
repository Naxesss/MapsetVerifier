// ReSharper disable once EmptyNamespace

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    // TODO: Figure out why this is commented. If I were to guess, it's because the resolution of elements depend on
    //       their scale in gameplay, which would cause both false positives and negatives.
    /*[Check]
    public class CheckSkinResolution : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Resources",
            Message = "Too high skin element resolution.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    "
                },
                {
                    "Reasoning",
                    @"
                    "
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Very high",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" greater than 2560 x 1440 ({1} x {2})",
                        "file name", "width", "height")
                    .WithCause(
                        "A skin element file has a width exceeding 2560 pixels or a height exceeding 1440 pixels.") },

                { "File size",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" has a file size exceeding 2.5 MB ({1} MB)",
                        "file name", "file size")
                    .WithCause(
                        "A skin element file has a file size greater than 2.5 MB.") },

                // parsing results
                { "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" leaves the current song folder, which shouldn't ever happen.",
                        "file name")
                    .WithCause(
                        "The file path of a skin element file starts with two dots.") },

                { "Missing",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" is missing, so unable to check that.",
                        "file name")
                    .WithCause(
                        "A skin element file referenced is not present.") },

                { "Exception",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" returned exception \"{1}\", so unable to check that.",
                        "file name", "exception")
                    .WithCause(
                        "An exception occurred trying to parse a skin element file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Common.TagFile tagFile in Common.GetTagFiles(beatmapSet, beatmapSet.GetUsedSkinImages()))
            {
                if (tagFile.file == null)
                {
                    yield return new Issue(TemplateFunc(tagFile.templateName), null,
                        tagFile.templateArgs.ToArray());
                    continue;
                }

                // Executes for each non-faulty background file used in one of the beatmaps in the set.
                List<Issue> issues = new List<Issue>();
                if (tagFile.file.Properties.PhotoWidth > 2560 ||
                    tagFile.file.Properties.PhotoHeight > 1440)
                {
                    issues.Add(new Issue(GetTemplate("Very high"), null,
                        tagFile.templateArgs[0],
                        tagFile.file.Properties.PhotoWidth,
                        tagFile.file.Properties.PhotoHeight));
                }

                // Most operating systems define 1 KB as 1024 B and 1 MB as 1024 KB,
                // not 10^(3x) which the prefixes usually mean, but 2^(10x), since binary is more efficient for circuits,
                // so since this is what your computer uses we'll use this too.
                double megaBytes = new FileInfo(tagFile.file.Name).Length / Math.Pow(1024, 2);
                if (megaBytes > 2.5)
                {
                    issues.Add(new Issue(GetTemplate("File size"), null,
                        tagFile.templateArgs[0],
                        FormattableString.Invariant($"{megaBytes:0.##}")));
                }

                return issues;
            }
        }
    }*/
}