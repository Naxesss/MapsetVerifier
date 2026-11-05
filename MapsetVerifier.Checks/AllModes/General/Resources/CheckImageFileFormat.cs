using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckImageFileFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Image file format differs from its file name.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring image files are named after the format they actually are in, for example not a PNG named `white.jpg`."
                    },
                    {
                        "Reasoning",
                        @"
                        The game looks at the content of an image, so such a file still shows up in-game. Other programs often go by the
                        file name instead, and can fail to open it or treat it incorrectly.

                        When renaming the file, make sure to also update wherever it is referenced, such as in the storyboard."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Format",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" is a {1} file, but its name says {2}. Rename it to match its format.",
                        "file name",
                        "actual format",
                        "format in the name"
                    ).WithCause(
                        "The content of an image file is a different format than its extension."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var songFilePath in beatmapSet.SongFilePaths)
            {
                var claimed = ImageFileFormat.FromExtension(songFilePath);
                if (claimed == null)
                    continue;

                var actual = ImageFileFormat.FromContent(songFilePath);
                if (actual == null || actual == claimed)
                    continue;

                yield return new Issue(
                    GetTemplate("Format"),
                    null,
                    songFilePath[(beatmapSet.SongPath.Length + 1)..].Replace('\\', '/'),
                    actual,
                    claimed
                );
            }
        }
    }
}
