using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckMappersGuildBackground : GeneralCheck
    {
        private static readonly string[] MappersGuildTags =
        [
            "mpg",
            "mappers guild",
            "mappers' guild",
            "mappersguild",
        ];

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Mappers' Guild tag present.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Informing that this beatmapset was created as part of a Mappers' Guild project, and
                        that its background needs to be free to use."
                    },
                    {
                        "Reasoning",
                        @"
                        Beatmapsets tagged with ""mpg"", ""mappers guild"", or ""mappers' guild"" were made
                        through a mappersguild.com community project. These projects require the background
                        image to be free to use, so this is useful context to be aware of during
                        modding/nomination."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "tag",
                    new IssueTemplate(
                        Issue.Level.Info,
                        "Make sure this background is free to use, or that you have permission from the artist."
                    ).WithCause(
                        "The tags field contains a Mappers' Guild tag (\"mpg\", \"mappers guild\", or \"mappers' guild\")."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps.FirstOrDefault();

            if (refBeatmap == null)
                yield break;

            var tags = refBeatmap.MetadataSettings.tags.ToLower();

            if (MappersGuildTags.Any(tag => tags.Contains(tag)))
                yield return new Issue(GetTemplate("tag"), null);
        }
    }
}
