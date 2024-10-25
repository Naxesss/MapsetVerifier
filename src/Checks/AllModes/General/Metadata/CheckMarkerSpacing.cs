using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Metadata
{
    [Check]
    public class CheckMarkerSpacing : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Metadata",
                Message = "Incorrect marker spacing.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/9w1fzvB.png
                        The romanized artist field is missing a whitespace before and after ""(CV:"". 
                        The unicode artist field is fine, though, since the characters are full-width.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the 
                    most part eliminated through standardization. Standardization also reduces confusion in case of 
                    multiple correct ways to write certain fields and contributes to making metadata more consistent 
                    across official content."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "Missing {0} in {1}; \"{2}\".", "something", "field", "field content").WithCause("Some whitespace or parameter is missing from the artist or title fields where only alphabetic " + "and numerical characters are involved.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "Missing {0} in {1}; \"{2}\".", "something", "field", "field content").WithCause("Same as the other check, but can involve any type of characters.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            var problemTests = new List<Func<string, string>>
            {
                field => EnsureSpaceBeforeAndAfter(field, @"CV:"),
                field => EnsureSpaceBeforeAndAfter(field, @"vs\."),
                field => EnsureSpaceBeforeAndAfter(field, @"feat\."),

                field => new Regex(@"[a-zA-Z0-9]\(CV:").IsMatch(field) ? "whitespace before \"(CV:\" or full-width bracket \"（\"" : null,
                field => new Regex(@"(?<!(\(|（))CV:").IsMatch(field) ? "\"(\" before \"CV:\"" : null,
                field => new Regex(@",[a-zA-Z0-9]").IsMatch(field) ? "whitespace after \",\"" : null
            };

            var warningTests = new List<Func<string, string>>
            {
                // Some artists include ampersand as part of their name.
                field => EnsureSpaceBeforeAndAfter(field, "&"),

                // Markers only need spaces around them if both of the following are true
                // - They are not full width (i.e. "、" for comma and "：" for colon)
                // - The character after/before is not full width (i.e. most chinese/japanese characters)
                // Source: Lanturn (metadata helper at the time of writing this)

                // The regex "[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]" matches all japanese characters.

                field => new Regex(@"(?<! |\(|（)feat\.").IsMatch(field) ? "whitespace before \"feat.\"" : null,
                field => new Regex(@"(?<! )(\(|（)feat\.").IsMatch(field) ? "whitespace before \"(feat.\"" : null,
                field => new Regex(@"(?<! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])vs\.").IsMatch(field) ? "whitespace before \"vs.\"" : null,
                field => new Regex(@"(?<! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])&").IsMatch(field) ? "whitespace before \"&\"" : null,

                field => new Regex(@"CV(?!:[ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]|：)").IsMatch(field) ? "whitespace after \"CV:\" or full-width colon \"：\"" : null,
                field => new Regex(@",(?![ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])").IsMatch(field) ? "whitespace after \",\" or full-width comma \"、\"" : null,

                field => new Regex(@"feat\.(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])").IsMatch(field) ? "whitespace after \"feat.\"" : null,
                field => new Regex(@"vs\.(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])").IsMatch(field) ? "whitespace after \"vs.\"" : null,
                field => new Regex(@"&(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])").IsMatch(field) ? "whitespace after \"&\"" : null
            };

            var metadata = refBeatmap.MetadataSettings;

            var fields = new List<Field>
            {
                new Field("artist", metadata.artist),
                new Field("artist unicode", metadata.artistUnicode)
            };

            var fieldIssues = new List<FieldIssue>();

            foreach (var field in fields)
            {
                // Old .osu versions didn't have unicode fields.
                if (field.content == null)
                    continue;

                foreach (var problemTest in problemTests)
                {
                    var message = problemTest(field.content);

                    if (message != null && !fieldIssues.Any(fieldIssue => fieldIssue.message == message && fieldIssue.field == field))
                        fieldIssues.Add(new FieldIssue(field, message, true));
                }

                foreach (var warningTest in warningTests)
                {
                    var message = warningTest(field.content);

                    if (message != null && !fieldIssues.Any(fieldIssue => fieldIssue.message == message && fieldIssue.field == field))
                        fieldIssues.Add(new FieldIssue(field, message, false));
                }
            }

            foreach (var fieldIssue in fieldIssues)
                yield return new Issue(GetTemplate(fieldIssue.isProblem ? "Problem" : "Warning"), null, fieldIssue.message, fieldIssue.field.name, fieldIssue.field.content);
        }

        /// <summary> Returns a message describing where a space is missing given a field and what is tested against. </summary>
        private static string EnsureSpaceBeforeAndAfter(string field, string test)
        {
            if (new Regex(test + "[a-zA-Z0-9]").IsMatch(field))
                return "whitespace after \"" + test.Replace("\\", "") + "\"";

            if (new Regex("[a-zA-Z0-9]" + test).IsMatch(field))
                return "whitespace before \"" + test.Replace("\\", "") + "\"";

            return null;
        }

        private class Field
        {
            public readonly string content;
            public readonly string name;

            public Field(string name, string content)
            {
                this.name = name;
                this.content = content;
            }
        }

        private class FieldIssue
        {
            public readonly Field field;
            public readonly bool isProblem;
            public readonly string message;

            public FieldIssue(Field field, string message, bool isProblem)
            {
                this.field = field;
                this.message = message;
                this.isProblem = isProblem;
            }
        }
    }
}