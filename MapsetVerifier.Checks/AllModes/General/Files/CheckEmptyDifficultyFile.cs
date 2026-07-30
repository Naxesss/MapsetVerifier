using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Files
{
    [Check]
    public class CheckEmptyDifficultyFile : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Files",
                Message = "Empty difficulty file.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring every .osu file in the song folder is a valid, complete difficulty."
                    },
                    {
                        "Reasoning",
                        @"
                        A .osu file with no content at all is not a usable difficulty; it has no metadata, no
                        hit objects, and cannot be loaded by the game."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Empty",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" is empty.", "path").WithCause(
                        "A .osu file in the song folder contains no data."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var fileName in beatmapSet.EmptyBeatmapFiles)
                yield return new Issue(GetTemplate("Empty"), null, fileName);
        }
    }
}
