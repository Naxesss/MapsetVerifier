using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckMultipleVideo : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Inconsistent video usage.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Making sure that any inconsistency in video is intentional and makes sense."
                    },
                    {
                        "Reasoning",
                        @"
                    When adding a guest difficulty or adding different mode difficulties, the mapset host may forget to ensure 
                    that the videos across beatmaps in the set are consistent. Using multiple videos can be fine in cases like 
                    compilations or if one difficulty is thematically different enough to warrant it, but do keep in mind that 
                    it takes up additional space.
                    <note>
                        For taiko, videos usually need to be modified in some way since they're only visible on the bottom half 
                        of the screen, so this check ignores any inconsistency with that mode from other modes.
                    </note>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Same Mode",
                    new IssueTemplate(Issue.Level.Warning, "{0} : {1}.", "path", "difficulties").WithCause("Difficulties of the same mode do not share the same video.")
                },

                {
                    "Cross Mode",
                    new IssueTemplate(Issue.Level.Warning, "Inconsistent video between the {0} and {1} beatmaps.", "mode", "mode").WithCause("Difficulties of separate modes (except taiko) do not share the same video.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var modeVideoPairs = new List<ModeVideoPair>();

            var modes = beatmapSet.Beatmaps.Select(beatmap => beatmap.GeneralSettings.mode).Distinct();

            foreach (var mode in modes)
            {
                var videoNames = beatmapSet.Beatmaps.Where(beatmap => beatmap.GeneralSettings.mode == mode).Select(beatmap => beatmap.Videos.FirstOrDefault()?.path ?? "None").Distinct().ToList();

                // It's possible the .osb file includes a video as well, which would run at the
                // same time as *any* .osu video file (either in front of or behind the other).
                var osbVideoPath = beatmapSet.Osb?.videos.FirstOrDefault()?.path;

                if (osbVideoPath != null && !videoNames.Contains(osbVideoPath))
                    videoNames.Add(osbVideoPath);

                foreach (var videoName in videoNames)
                {
                    var suchBeatmaps = beatmapSet.Beatmaps.Where(beatmap => (beatmap.Videos.FirstOrDefault()?.path ?? "None") == videoName || beatmapSet.Osb?.videos.FirstOrDefault()?.path == videoName).ToList();

                    if (videoNames.Count > 1 && suchBeatmaps.Any())
                        yield return new Issue(GetTemplate("Same Mode"), null, videoName, string.Join(", ", suchBeatmaps));

                    if (!modeVideoPairs.Any(pair => pair.mode == mode && pair.videoName == videoName))
                        modeVideoPairs.Add(new ModeVideoPair(mode, videoName));
                }
            }

            foreach (var issue in GetCrossModeIssues(modeVideoPairs))
                yield return issue;
        }

        private IEnumerable<Issue> GetCrossModeIssues(IReadOnlyList<ModeVideoPair> modeVideoPairs)
        {
            var inconsistentModes = new List<(Beatmap.Mode, Beatmap.Mode)>();

            for (var i = 0; i < modeVideoPairs.Count - 1; ++i)
                for (var j = i; j < modeVideoPairs.Count; ++j)
                {
                    var pair = modeVideoPairs[i];
                    var otherPair = modeVideoPairs[j];

                    // We're only looking for inconsistenties between modes here.
                    if (pair.mode == otherPair.mode || pair.videoName == otherPair.videoName)
                        continue;

                    // Taiko generally does not include videos due to their playfield covering it, hence ignoring inconsistencies.
                    if (pair.mode == Beatmap.Mode.Taiko || otherPair.mode == Beatmap.Mode.Taiko)
                        continue;

                    // Only mention this once for each combination of modes.
                    if (inconsistentModes.Contains((pair.mode, otherPair.mode)) || inconsistentModes.Contains((otherPair.mode, pair.mode)))
                        continue;

                    inconsistentModes.Add((pair.mode, otherPair.mode));

                    yield return new Issue(GetTemplate("Cross Mode"), null, pair.mode.ToString().ToLower(), otherPair.mode.ToString().ToLower());
                }
        }

        private readonly struct ModeVideoPair
        {
            public readonly Beatmap.Mode mode;
            public readonly string videoName;

            public ModeVideoPair(Beatmap.Mode mode, string videoName)
            {
                this.mode = mode;
                this.videoName = videoName;
            }
        }
    }
}