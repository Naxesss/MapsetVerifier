using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Resources
{
    [Check]
    public class CheckBgPresence : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Resources",
                Message = "Missing background.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring that each beatmap in a beatmapset has a background image present.

                        ![](https://i.imgur.com/P9TdA7K.jpg)
                        An example of a default non-seasonal background as shown in the editor."
                    },
                    {
                        "Reasoning",
                        @"
                        Backgrounds help players recognize the beatmap, and the absence of one makes it look incomplete."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "All",
                    new IssueTemplate(Issue.Level.Problem, "All difficulties are missing backgrounds.")
                        .WithCause("None of the difficulties have a background present.")
                },

                {
                    "One",
                    new IssueTemplate(Issue.Level.Problem, "{0} has no background.", "difficulty")
                        .WithCause("One or more difficulties are missing backgrounds, but not all.")
                },

                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Problem, "{0} is missing its background file, \"{1}\".", "difficulty", "path")
                        .WithCause("A background file path is present, but no file exists where it is pointing.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.Beatmaps.All(beatmap => beatmap.Backgrounds.Count == 0))
            {
                yield return new Issue(GetTemplate("All"), null);

                yield break;
            }

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (beatmap.Backgrounds.Count == 0)
                {
                    yield return new Issue(GetTemplate("One"), null, beatmap.MetadataSettings.version);

                    continue;
                }

                if (beatmapSet.SongPath == null)
                    continue;

                foreach (var bg in beatmap.Backgrounds)
                {
                    var path = beatmapSet.SongPath + Path.DirectorySeparatorChar + bg.path;

                    if (!File.Exists(path))
                        yield return new Issue(GetTemplate("Missing"), null, beatmap.MetadataSettings.version, bg.path);
                }
            }
        }
    }
}