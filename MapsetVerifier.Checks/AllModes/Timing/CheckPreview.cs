using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Timing
{
    [Check]
    public class CheckPreview : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Category = "Timing",
                Message = "Inconsistent or unset preview time.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring that preview times are set and consistent for all beatmaps in the set."
                    },
                    {
                        "Reasoning",
                        @"
                        Without a set preview time the game will automatically pick a point to use as preview, but this rarely aligns with 
                        any beat or start of measure in the song. Additionally, not selecting a preview point will cause the web to use the 
                        whole song as preview, rather than the usual 10 second limit. Which difficulty is used to take preview time from is 
                        also not necessarily consistent between the web and the client.

                        Similarly to metadata and timing, preview points should really just be a global setting for the whole beatmapset and 
                        not difficulty-specific."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Not Set",
                    new IssueTemplate(Issue.Level.Problem, "Preview time is not set.")
                        .WithCause("The preview time of a beatmap is missing.")
                },

                {
                    "Inconsistent",
                    new IssueTemplate(Issue.Level.Problem, "Preview time is inconsistent, see {0}.", "difficulty")
                        .WithCause("The preview time of a beatmap is different from the reference beatmap.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var refBeatmap = beatmapSet.Beatmaps[0];

            foreach (var beatmap in beatmapSet.Beatmaps)
                // Here we do care if the floats differ. It should be exactly -1. Anything else is treated as an actual offset.
                // ReSharper disable twice CompareOfFloatsByEqualityOperator
                if (beatmap.GeneralSettings.previewTime == -1)
                    yield return new Issue(GetTemplate("Not Set"), beatmap);

                else if (beatmap.GeneralSettings.previewTime != refBeatmap.GeneralSettings.previewTime)
                    yield return new Issue(GetTemplate("Inconsistent"), beatmap, refBeatmap);
        }
    }
}