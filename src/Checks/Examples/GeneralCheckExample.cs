using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Examples
{
    // This attribute tells the framework that it's a check it should register.
    // Since this is just an example class, we're not going to register this.
    // [Check]
    public class GeneralCheckExample : GeneralCheck
    {
        /// <summary>
        ///     Determines which modes the check shows for, in which category the check appears, the message for the check,
        ///     etc.
        /// </summary>
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Example",
                Message = "Difficulty names are present in the beatmap.",
                Author = "Naxess",
                Documentation = new Dictionary<string, string>
                {
                    { "Purpose", "Show an example of a custom general check." },
                    { "Reasoning", "Examples teach through practice." }
                }
            };

        /// <summary>
        ///     Returns a dictionary of issue templates, which determine how each sub-issue is formatted, the issue level,
        ///     etc.
        /// </summary>
        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "DiffName",
                    new IssueTemplate(Issue.Level.Warning, "One of the difficulty names is {0}.", "difficulty name")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
                yield return new Issue(GetTemplate("DiffName"), null, beatmap.MetadataSettings.version);
        }
    }
}