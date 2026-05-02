using System.Text.RegularExpressions;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.Files
{
    [Check]
    public class CheckHitsoundDiff : BeatmapSetCheck 
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata
        {
            Modes = [Beatmap.Mode.Mania],
            Category = "Files",
            Message = "Hitsound difficulty present.",
            Author = "RandomeLoL",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Beatmaps must not be nominated with hitsound difficulties still present."
                },
                {
                    "Reasoning",
                    @"
                    Hitsounding template difficulties are commonly used in osu!mania as an easy way to copy and apply hitsounding across all difficulties of the beatmap. However, they must be deleted before nominating the beatmap.
                    "
                }
            }
        };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                "HitsoundDiff",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} may be a hitsound difficulty. If it were the case, ensure it is deleted before nominating this beatmap.", "difficulty")
                    .WithCause("Potential Hitsound difficulty detected.")
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (Beatmap beatmap in beatmapSet.Beatmaps) 
            {
                string difficulty = beatmap.MetadataSettings.version;
                if (Regex.IsMatch(difficulty, WildCardToRegular("*hit*sound*"), RegexOptions.IgnoreCase) | 
                    Regex.IsMatch(difficulty, WildCardToRegular("_hs_"), RegexOptions.IgnoreCase)) 
                        yield return new Issue(GetTemplate("HitsoundDiff"), beatmap, difficulty);
            }
        }
    }
}
