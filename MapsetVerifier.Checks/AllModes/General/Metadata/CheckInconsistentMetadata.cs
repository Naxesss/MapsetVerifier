using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckInconsistentMetadata : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Inconsistent metadata.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Keeping metadata consistent between all difficulties of a beatmapset.

                        ![](https://i.imgur.com/ojdxg6z.png)
                        Comparing two difficulties with different titles in a beatmapset."
                    },
                    {
                        "Reasoning",
                        @"
                        Since all difficulties are of the same song, they should use the same song metadata. The website also assumes it's all the 
                        same, so it'll only display one of the artists, titles, creators, etc. Multiple metadata simply isn't supported very well, 
                        and should really just be a global beatmap setting rather than a .osu-specific one."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Tags",
                    new IssueTemplate(Issue.Level.Problem, "Inconsistent tags between {0} and {1}, difference being \"{2}\".", "difficulty", "difficulty", "difference")
                        .WithCause("A tag is present in one difficulty but missing in another.\n" +
                                   "> Does not care which order the tags are written in or about duplicate tags, simply that the tags themselves are consistent.")
                },

                {
                    "Other Field",
                    new IssueTemplate(Issue.Level.Problem, "Inconsistent {0} fields between {1} and {2}; \"{3}\" and \"{4}\" respectively.", "field", "difficulty", "difficulty", "field", "field")
                        .WithCause("A metadata field is not consistent between all difficulties.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];
            var refVersion = refBeatmap.MetadataSettings.version;
            var refSettings = refBeatmap.MetadataSettings;

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var curVersion = beatmap.MetadataSettings.version;

                var issues = new List<Issue>();

                issues.AddRange(GetInconsistency("artist", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.artist));

                issues.AddRange(GetInconsistency("unicode artist", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.artistUnicode));

                issues.AddRange(GetInconsistency("title", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.title));

                issues.AddRange(GetInconsistency("unicode title", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.titleUnicode));

                issues.AddRange(GetInconsistency("source", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.source));

                issues.AddRange(GetInconsistency("creator", beatmap, refBeatmap, otherBeatmap => otherBeatmap.MetadataSettings.creator));

                foreach (var issue in issues)
                    yield return issue;

                if (beatmap.MetadataSettings.tags == refSettings.tags)
                    continue;

                IEnumerable<string> refTags = refSettings.tags.Split(' ');
                IEnumerable<string> curTags = beatmap.MetadataSettings.tags.Split(' ');
                var differenceTags = refTags.Except(curTags).Union(curTags.Except(refTags)).Distinct();

                var difference = string.Join(" ", differenceTags);

                if (difference != "")
                    yield return new Issue(GetTemplate("Tags"), null, curVersion, refVersion, difference);
            }
        }

        /// <summary> Returns issues where the metadata fields of the given beatmaps do not match. </summary>
        private IEnumerable<Issue> GetInconsistency(string fieldName, Beatmap beatmap, Beatmap otherBeatmap, Func<Beatmap, string> metadataField)
        {
            var field = metadataField(beatmap);
            var otherField = metadataField(otherBeatmap);

            if ((field ?? "") != (otherField ?? ""))
                yield return new Issue(GetTemplate("Other Field"), null, fieldName, beatmap, otherBeatmap, field, otherField);
        }
    }
}