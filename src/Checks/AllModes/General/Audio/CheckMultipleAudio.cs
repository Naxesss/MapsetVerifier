using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckMultipleAudio : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Audio",
                Message = "Multiple or missing audio files.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that each beatmapset only contains one audio file."
                    },
                    {
                        "Reasoning",
                        @"
                    Although this works well in-game, the website preview, metadata, tags, etc are all relying on that each 
                    beatmapset is based around a single song. As such, having multiple songs in a single beatmapset is not 
                    supported properly. Each song will also need its own spread, so having each set of difficulties in its 
                    own beatmapset makes things more organized."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Multiple",
                    new IssueTemplate(Issue.Level.Problem, "{0}", "audio file : difficulties").WithCause("There is more than one audio file used between all difficulties.")
                },

                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Problem, "No audio file could be found.").WithCause("There is no audio file used in any difficulty.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.Beatmaps.All(beatmap => beatmap.GetAudioFilePath() == null))
            {
                foreach (var beatmap in beatmapSet.Beatmaps)
                    yield return new Issue(GetTemplate("Missing"), beatmap);
            }
            else
            {
                var issues = Common.GetInconsistencies(beatmapSet, beatmap => beatmap.GetAudioFilePath() != null ? PathStatic.RelativePath(beatmap.GetAudioFilePath(), beatmap.SongPath) : "None", GetTemplate("Multiple"));

                foreach (var issue in issues)
                    yield return issue;
            }
        }
    }
}