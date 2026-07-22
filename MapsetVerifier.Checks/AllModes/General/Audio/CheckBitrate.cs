using ManagedBass;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckBitrate : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Audio",
                Message = "Too high or low audio bitrate.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing audio quality from being noticably low or unnoticably high to save on file size.
                        
                        ![](assets/checks/all-modes-general-audio-bitrate-1.png ""Audio bitrate as shown in the properties of a file. For some files this is not visible."")
                        "
                    },
                    {
                        "Reasoning",
                        @"
                        Once you get lower than 128 kbps the quality loss is usually quite noticeable. After 192 kbps, with the 
                        setup of the average player, it would be difficult to tell a difference and as such would also be a 
                        waste of resources.

                        > Should no higher quality be available anywhere, less than 128 kbps may be acceptable depending on how noticeable it is.
                        ---
                        Audio files in the OGG container format using the Q6 setting will typically land around 192 kbps, 
                        somtimes inconveniently a little above that, so the upper limit for OGG is instead 208 kbps.
                        
                        > Q6 generally reads as 192 kbps in tools like Spek. These programs actually just approximate the bitrate by rounding to the closest 32 kbps step. 192 kbps is one such step. Therefore, any file > 208 kbps will show as 224 kbps in these tools."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Bitrate",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "Average audio bitrate for \"{0}\", {1} kbps, is too {2}.",
                        "path",
                        "bitrate",
                        "high/low"
                    ).WithCause(
                        "The average bitrate of an audio file is either higher than 192/208 kbps for MP3/OGG respectively or lower than 128 kbps."
                    )
                },
                {
                    "Exception",
                    new IssueTemplate(
                        Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path"
                    ).WithCause("A file which was attempted to be checked could not be opened.")
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var issue in GetAudioIssues(beatmapSet))
                yield return issue;
        }

        private IEnumerable<Issue> GetAudioIssues(BeatmapSet beatmapSet)
        {
            var audioPath = beatmapSet.GetAudioFilePath();

            if (audioPath == null)
                yield break;

            ChannelType audioFormat = 0;
            Issue? errorIssue = null;

            try
            {
                audioFormat = AudioBASS.GetFormat(audioPath);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Couldn't check audio file");
                errorIssue = new Issue(
                    GetTemplate("Exception"),
                    null,
                    PathStatic.RelativePath(audioPath, beatmapSet.SongPath)
                );
            }

            if (errorIssue != null)
                yield return errorIssue;

            var upperBitrateLimit = audioFormat == ChannelType.OGG ? 208 : 192;

            // `Audio.GetBitrate` has a < 0.1 kbps error margin, so we should round this.
            var bitrate = Math.Round(AudioBASS.GetBitrate(audioPath));

            if (bitrate >= 128 && bitrate <= upperBitrateLimit)
                yield break;

            var relativePath = PathStatic.RelativePath(audioPath, beatmapSet.SongPath);

            yield return new Issue(
                GetTemplate("Bitrate"),
                null,
                relativePath,
                $"{bitrate:0.##}",
                bitrate < 128 ? "low" : "high"
            );
        }
    }
}
