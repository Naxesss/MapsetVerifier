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
        [
            ["Video", "Game"],
            ["Anime"],
            ["Rock"],
            ["Pop"],
            ["Novelty"],
            ["Hip", "Hop"],
            ["Electronic"],
            ["Metal"],
            ["Classical"],
            ["Folk"],
            ["Jazz"]
        ];

        private static readonly string[][] LanguageTagCombinations =
        [
            ["English"],
            ["Chinese"],
            ["French"],
            ["German"],
            ["Italian"],
            ["Japanese"],
            ["Korean"],
            ["Spanish"],
            ["Swedish"],
            ["Russian"],
            ["Polish"],
            ["Instrumental"],

            // Following are not web languages, but if we find these in the tags,
            // web would need to be "Other" anyway, so no point in warning.
            ["Conlang"],
            ["Hindi"],
            ["Arabic"],
            ["Portugese"],
            ["Turkish"],
            ["Vietnamese"],
            ["Persian"],
            ["Indonesian"],
            ["Ukrainian"],
            ["Romanian"],
            ["Dutch"],
            ["Thai"],
            ["Greek"],
            ["Somali"],
            ["Malay"],
            ["Hungarian"],
            ["Czech"],
            ["Norwegian"],
            ["Finnish"],
            ["Danish"],
            ["Latvia"],
            ["Lithuanian"],
            ["Estonian"],
            ["Punjabi"],
            ["Bengali"]
        ];

        public override CheckMetadata GetMetadata() =>
            new()
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

                        ![](https://i.imgur.com/g6zlqhy.png)
                        An example of web genre/language also in the tags."
                    }
                }
            };

        private static string ToCause(IEnumerable<string[]> tagCombinations)
        {
            var liStr = new StringBuilder();

            foreach (var tags in tagCombinations)
                liStr.Append("- " + string.Join(" & ", tags.Select(tag => "\"" + tag + "\"\n")));

            return $"{liStr}";
        }

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Genre",
                    new IssueTemplate(Issue.Level.Warning, "Missing genre tag (\"rock\", \"pop\", \"electronic\", etc), ignore if none fit.")
                        .WithCause("None of the following tags were found (case insensitive):" + ToCause(GenreTagCombinations))
                },

                {
                    "Language",
                    new IssueTemplate(Issue.Level.Warning, "Missing language tag (\"english\", \"japanese\", \"instrumental\", etc), ignore if none fit.")
                        .WithCause("None of the following tags were found (case insensitive):" + ToCause(LanguageTagCombinations))
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