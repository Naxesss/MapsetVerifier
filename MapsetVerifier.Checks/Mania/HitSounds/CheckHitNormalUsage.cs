using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using static MapsetVerifier.Checks.Utils.ManiaUtils;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    [Check]
    public class CheckHitNormalUsage : BeatmapSetCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Mania],
                Category = "Hit Sounds",
                Message = "Missing custom hitnormal",
                Author = "RandomeLoL",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Maps must always have a hitnormal sample."
                    },
                    {
                        "Reasoning",
                        @"
                    To have a better playing experience, some players prefer active hitsound feedback. osu!mania Ranking Criteria mandates maps to have at least a custom sample overriding osu!'s default hitnormal."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>
            {
                {
                    "HitnormalFile",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "No hitnormal sample found in beatmap folder"
                    ).WithCause("Cannot find a hitnormal sample in the beatmap folder.")
                },
                {
                    "HitnormalOverride",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Custom hitnormal isn't being overriden."
                    ).WithCause(
                        "A hitnormal file is present, but it's not overwriting the default hitnormal."
                    )
                },
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var maniaBeatmaps = beatmapSet
                .Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                .ToList();

            // No hitnormal sample found in beatmapSet's folder
            var hitnormalList = getHitNormalSamples(beatmapSet.HitSoundFiles).ToList();
            if (hitnormalList.Count == 0)
            {
                foreach (var beatmap in maniaBeatmaps)
                {
                    yield return new Issue(GetTemplate("HitnormalFile"), beatmap);
                }
            }
            // A hitnormal has been found. Check whether it is overriding it.
            else
            {
                foreach (var beatmap in maniaBeatmaps)
                {
                    foreach (var timingLine in beatmap.TimingLines)
                    {
                        if (timingLine.Sampleset == HitSample.SamplesetType.Auto)
                            yield return new Issue(GetTemplate("HitnormalOverride"), beatmap);

                        var customIndex =
                            timingLine.CustomIndex <= 1 ? "" : timingLine.CustomIndex.ToString();
                        var sample =
                            timingLine.Sampleset.ToString().ToLower() + "-hitnormal" + customIndex;

                        if (!isHitNormalInList(sample, hitnormalList))
                            yield return new Issue(GetTemplate("HitnormalOverride"), beatmap);
                    }
                }
            }
        }
    }
}
