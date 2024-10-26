using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Examples
{
    // This attribute tells the framework that it's a check it should register.
    // Since this is just an example class, we're not going to register this.
    // [Check]
    public class CheckExample : BeatmapCheck
    {
        /// <summary>
        ///     Determines which modes the check shows for, in which category the check appears, the message for the check,
        ///     etc.
        /// </summary>
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = new[]
                {
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Catch
                },
                Difficulties = new[]
                {
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal,
                    Beatmap.Difficulty.Hard
                },
                Category = "Example",
                Message = "Difficulty name is present in the beatmap.",
                Author = "Naxess",
                Documentation = new Dictionary<string, string>
                {
                    { "Purpose", "Show an example of a custom check." },
                    { "Reasoning", "Examples teach through practice." }
                }
            };

        /// <summary>
        ///     Returns a dictionary of issue templates, which determine how each sub-issue is formatted, the issue level,
        ///     etc.
        /// </summary>
        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "DiffName",
                    new IssueTemplate(Issue.Level.Warning, "The difficulty name is {0}.", "difficulty name")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            yield return new Issue(GetTemplate("DiffName"), beatmap, beatmap.MetadataSettings.version);
        }
    }
}