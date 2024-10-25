using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using TagLib;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckAudioInVideo : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new CheckMetadata
            {
                Category = "Audio",
                Message = "Audio channels in video.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Reducing the file size of videos."
                    },
                    {
                        "Reasoning",
                        @"
                    The audio track of videos will not play and usually take a similar amount of file size as any other audio file, 
                    so not removing the audio track means a noticeable amount of resources are wasted, even if the audio track is 
                    empty but still present."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new Dictionary<string, IssueTemplate>
            {
                {
                    "Audio",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\"", "path").WithCause("An audio track is present in one of the video files.")
                },

                {
                    "Leaves Folder",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" leaves the current song folder, which shouldn't ever happen.", "path").WithCause("The file path of a video file starts with two dots.")
                },

                {
                    "Missing",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" is missing, so unable to check that. Make sure you've downloaded with video.", "path").WithCause("A video file referenced is not present.")
                },

                {
                    "Exception",
                    new IssueTemplate(Issue.Level.Error, Common.FILE_EXCEPTION_MESSAGE, "path", "exception info").WithCause("An exception occurred trying to parse a video file.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var issue in Common.GetTagOsuIssues(beatmapSet, beatmap => beatmap.Videos.Count > 0 ? beatmap.Videos.Select(video => video.path) : null, GetTemplate, tagFile =>
                     {
                         // Executes for each non-faulty video file used in one of the beatmaps in the set.
                         var issues = new List<Issue>();

                         if (tagFile.file.Properties.MediaTypes.HasFlag(MediaTypes.Video) && tagFile.file.Properties.AudioChannels > 0)
                             issues.Add(new Issue(GetTemplate("Audio"), null, tagFile.templateArgs[0]));

                         return issues;
                     }))
                // Returns issues from both non-faulty and faulty files.
                yield return issue;
        }
    }
}