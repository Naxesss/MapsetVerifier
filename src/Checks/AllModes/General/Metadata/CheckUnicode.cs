using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckUnicode : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Metadata",
                Message = "Unicode in romanized fields.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that all characters in the romanized metadata fields can be displayed and communicated properly across 
                    multiple operating systems, devices and internet protocols.
                    <image>
                        https://i.imgur.com/3UAwC97.png
                        A beatmap with its unicode title manually edited into its romanized title field.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    The romanized title, artist, creator and difficulty name fields are used in the file name of the .osu and .osb, as well as by 
                    the website to allow for updates and syncing. As such, if they contain invalid characters, the beatmapset may become corrupt 
                    when uploaded, preventing users from properly downloading it.
                    <br \><br \>
                    Even if it were possible to download correctly, should a character be unsupported it will be displayed as a box, questionmark 
                    or other placeholder character in-game, which makes some titles and artists impossible to interpret and distinguish.
                    <br \><br \>
                    Some unicode characters do seem to work, however. The title and artist fields are regulated by the Ranking Criteria, and 
                    creator names filtered upon creation, but difficulty names are not regulated or filtered by anything, so we do those case by 
                    case."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} field contains unicode characters,\"{1}\", those being \"{2}\".", "Artist/title/creator", "field", "unicode char(s)").WithCause("The romanized title, artist, or creator field contains unicode characters.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} field contains unicode characters,\"{1}\", those being \"{2}\". If the map can still be downloaded this is probably ok.", "difficulty name", "field", "unicode char(s)").WithCause("The difficulty name field contains unicode characters.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                foreach (var issue in GetUnicodeIssues("Difficulty name", beatmap.MetadataSettings.version, "Warning"))
                    yield return issue;

                foreach (var issue in GetUnicodeIssues("Romanized title", beatmap.MetadataSettings.title))
                    yield return issue;

                foreach (var issue in GetUnicodeIssues("Romanized artist", beatmap.MetadataSettings.artist))
                    yield return issue;

                foreach (var issue in GetUnicodeIssues("Creator", beatmap.MetadataSettings.creator))
                    yield return issue;
            }
        }

        private IEnumerable<Issue> GetUnicodeIssues(string fieldName, string field, string template = "Problem")
        {
            if (ContainsUnicode(field))
                yield return new Issue(GetTemplate(template), null, fieldName, field, GetUnicodeCharacters(field));
        }

        private static bool IsUnicode(char ch) => ch > 127;

        private static bool ContainsUnicode(string str) => str.Any(IsUnicode);

        private static string GetUnicodeCharacters(string str) => string.Join("", str.Where(IsUnicode));
    }
}