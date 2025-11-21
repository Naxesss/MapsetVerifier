using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckVideoOffset : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Inconsistent video offset.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring that the video aligns with the song consistently for all difficulties.

                        ![](https://i.imgur.com/RDRL3qG.png)
                        Two difficulties with different video offsets, as shown in the respective .osu files. The second 
                        argument, after ""Video"", is the offset in ms."
                    },
                    {
                        "Reasoning",
                        @"
                        Since many videos tend to match the music in some way, for example do transitions on downbeats, it wouldn't 
                        make much sense having difficulty-dependent video offsets, as all difficulties are based around the same song 
                        starting at the same point in time."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Multiple",
                    new IssueTemplate(Issue.Level.Problem, "{0}", "video offset : difficulties")
                        .WithCause("There is more than one video offset used between all difficulties.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var issue in Common.GetInconsistencies(beatmapSet, beatmap => beatmap.Videos.Count > 0 ? beatmap.Videos[0].offset.ToString() : null, GetTemplate("Multiple")))
                yield return issue;
        }
    }
}