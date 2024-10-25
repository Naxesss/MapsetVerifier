using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckGenreLanguage : GeneralCheck
    {
        private static readonly string[][] GenreTagCombinations =
        {
            new[] { "Video", "Game" },
            new[] { "Anime" },
            new[] { "Rock" },
            new[] { "Pop" },
            new[] { "Novelty" },
            new[] { "Hip", "Hop" },
            new[] { "Electronic" },
            new[] { "Metal" },
            new[] { "Classical" },
            new[] { "Folk" },
            new[] { "Jazz" }
        };

        private static readonly string[][] LanguageTagCombinations =
        {
            new[] { "English" },
            new[] { "Chinese" },
            new[] { "French" },
            new[] { "German" },
            new[] { "Italian" },
            new[] { "Japanese" },
            new[] { "Korean" },
            new[] { "Spanish" },
            new[] { "Swedish" },
            new[] { "Russian" },
            new[] { "Polish" },
            new[] { "Instrumental" },

            // Following are not web languages, but if we find these in the tags,
            // web would need to be "Other" anyway, so no point in warning.
            new[] { "Conlang" },
            new[] { "Hindi" },
            new[] { "Arabic" },
            new[] { "Portugese" },
            new[] { "Turkish" },
            new[] { "Vietnamese" },
            new[] { "Persian" },
            new[] { "Indonesian" },
            new[] { "Ukrainian" },
            new[] { "Romanian" },
            new[] { "Dutch" },
            new[] { "Thai" },
            new[] { "Greek" },
            new[] { "Somali" },
            new[] { "Malay" },
            new[] { "Hungarian" },
            new[] { "Czech" },
            new[] { "Norwegian" },
            new[] { "Finnish" },
            new[] { "Danish" },
            new[] { "Latvia" },
            new[] { "Lithuanian" },
            new[] { "Estonian" },
            new[] { "Punjabi" },
            new[] { "Bengali" }
        };

        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Metadata",
                Message = "Missing genre/language in tags.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Consistent searching between web and in-game."
                    },
                    {
                        "Reasoning",
                        @"
                    Web's language/genre fields can be searched for on the web, but not in-game. 
                    They are therefore added to the tags for consistency.
                    <image-right>
                        https://i.imgur.com/g6zlqhy.png
                        An example of web genre/language also in the tags.
                    </image>"
                    }
                }
            };

        private static string ToCause(IEnumerable<string[]> tagCombinations)
        {
            var liStr = new StringBuilder();

            foreach (var tags in tagCombinations)
                liStr.Append("<li>" + string.Join(" & ", tags.Select(tag => "\"" + tag + "\"")) + "</li>");

            return $"<ul>{liStr}</ul>";
        }

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Genre",
                    new IssueTemplate(Issue.Level.Warning, "Missing genre tag (\"rock\", \"pop\", \"electronic\", etc), ignore if none fit.").WithCause("None of the following tags were found (case insensitive):" + ToCause(GenreTagCombinations))
                },

                {
                    "Language",
                    new IssueTemplate(Issue.Level.Warning, "Missing language tag (\"english\", \"japanese\", \"instrumental\", etc), ignore if none fit.").WithCause("None of the following tags were found (case insensitive):" + ToCause(LanguageTagCombinations))
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps.FirstOrDefault();

            if (refBeatmap == null)
                yield break;

            var tags = refBeatmap.MetadataSettings.tags.ToLower().Split(" ");

            if (!HasAnyCombination(GenreTagCombinations, tags))
                yield return new Issue(GetTemplate("Genre"), null);

            if (!HasAnyCombination(LanguageTagCombinations, tags))
                yield return new Issue(GetTemplate("Language"), null);
        }

        /// <summary>
        ///     Returns true if all tags in any combination exist in the given tags
        ///     (e.g. contains both "Video" and "Game", or "Electronic"), case insensitive.
        /// </summary>
        private static bool HasAnyCombination(IEnumerable<string[]> tagCombinations, IEnumerable<string> tags) =>
            tagCombinations.Any(tagCombination => tagCombination.All(tagInCombination => tags.Any(tag => tag.Contains(tagInCombination.ToLower()))));
    }
}