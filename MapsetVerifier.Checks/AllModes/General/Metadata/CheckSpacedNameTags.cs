using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckSpacedNameTags : GeneralCheck
    {
        // Two single characters in a row also happen between unrelated tags, like "c c#" or "2 4".
        private const int MinCharacters = 3;

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Metadata",
                Message = "Names spread over single character tags.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out names with spaces between single characters that aren't kept together in the tags,
                        like *-[M o c h a]-* instead of *-[M_o_c_h_a]-*."
                    },
                    {
                        "Reasoning",
                        @"
                        Tags are separated by spaces, so such a name ends up as a row of single characters that can't be
                        searched for. Underscores keep the name together as one tag.

                        Only three or more characters in a row are reported, since fewer are often unrelated tags."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" in the tags should be \"{1}\", ignore if not a name.",
                        "tags",
                        "tag"
                    )
                        .WithCause(
                            "Three or more tags in a row each have only a single letter, besides punctuation around them."
                        )
                        .WithRule(RC.Metadata.Tags_NamesSpacesBetweenSingleCharacters)
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var names = beatmapSet
                .Beatmaps.Select(beatmap => beatmap.MetadataSettings.tags)
                .Distinct()
                .SelectMany(FindSpacedNames)
                .Distinct();

            foreach (var name in names)
                yield return new Issue(GetTemplate("Warning"), null, name, name.Replace(' ', '_'));
        }

        private static IEnumerable<string> FindSpacedNames(string tags)
        {
            var name = new List<string>();
            foreach (var tag in tags.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (IsSingleCharacter(tag))
                {
                    name.Add(tag);
                    continue;
                }

                if (name.Count >= MinCharacters)
                    yield return string.Join(' ', name);
                name.Clear();
            }

            if (name.Count >= MinCharacters)
                yield return string.Join(' ', name);
        }

        // "M", "-[M" and "a]-", but not "&" or "2". Other scripts have single character words.
        private static bool IsSingleCharacter(string tag) =>
            tag.Count(char.IsAsciiLetter) == 1
            && tag.All(ch =>
                char.IsAsciiLetter(ch)
                || (char.IsAscii(ch) && (char.IsPunctuation(ch) || char.IsSymbol(ch)))
            );
    }
}
