using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Taiko.Timing
{
    [Check]
    public class CheckKiaiConsistency : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "SN707, Nostril",
                Category = "Compose",
                Message = "Kiai inconsistencies",
                Modes = [Beatmap.Mode.Taiko],
                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Kiais should be consistent between difficulties."
                    },
                    {
                        "Reasoning",
                        @"
                    Kiais in osu!taiko have more impact than in other modes (illuminates entire playfield, slight score boost), so consistency is important unless there's a valid reason to break it (i.e. GDs)."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Inconsistent",
                    new IssueTemplate(
                            Issue.Level.Minor,
                        "Group {0}: ({1})",
                        "Kiai Group #",
                        "List of Difficulties")
                    .WithCause("Kiai start and end times are not aligned across difficulties.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            // Store all kiai times in a Dictionary
            var mapsetKiais = new Dictionary<string, List<string>>();

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                var beatmapKiais = string.Join(',', beatmap.GetKiaiToggles().Select(x => x.Offset));
                mapsetKiais.TryAdd(beatmapKiais, new List<string>());
                mapsetKiais[beatmapKiais].Add(beatmap.MetadataSettings.version);
            }

            int mapsetKiaiSetCount = mapsetKiais.Count;
            if (mapsetKiaiSetCount > 1)
            {
                // At least 2 different 'sets' of kiais
                // Emit each of them and exit
                var groupStrings = new List<string>();
                List<List<string>> diffNameStrings = mapsetKiais.Values.ToList();
                for (int i = 0; i < mapsetKiaiSetCount; i++)
                {
                    yield return new Issue(
                        GetTemplate("Inconsistent"),
                        null,
                        (i + 1).ToString(),
                        string.Join(", ", diffNameStrings[i])
                    );
                }
            }
        }
    }
}
