using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.HitSounds
{
    [Check]
    public class CheckSilentHitSounds : GeneralCheck
    {
        private const long BlankHitSoundSize = 44;
        private const float SilenceThreshold = 0.01f;

        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Hit Sounds",
                Message = "Silent hit sounds not using the 44-byte blank.wav.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring silent hit sounds use the required 44-byte file rather than a larger file padded with silence."
                    },
                    {
                        "Reasoning",
                        @"
                        The ranking criteria requires completely silent sound files to use [this 44-byte file](https://up.ppy.sh/files/blank.wav) 
                        which osu provides. Other files have unnecessarily large file sizes, and 0-byte files do not function.

                        Mappers occasionally create silent hit sounds by exporting an audio file with no audible content rather than using 
                        the provided blank.wav. While these files play correctly in-game, they bloat the beatmap's download size for no benefit."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Silent",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "\"{0}\" is a silent hit sound but is not the required 44-byte blank.wav.",
                        "path"
                    ).WithCause(
                        "A used hit sound file is effectively silent but does not match the 44-byte file osu provides for muted hit sounds."
                    )
                },
                {
                    "Unable to check",
                    new IssueTemplate(
                        Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path"
                    ).WithCause("There was an error parsing a hit sound file.")
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var hsFile in beatmapSet.HitSoundFiles)
            {
                var hsPath = Path.Combine(beatmapSet.SongPath, hsFile);

                long length = 0;
                List<float[]> peaks = [];
                Exception? exception = null;

                try
                {
                    length = new FileInfo(hsPath).Length;

                    // 0-byte files are reported by CheckZeroBytes, and the official blank.wav is exactly 44 bytes.
                    if (length != 0 && length != BlankHitSoundSize)
                        peaks = AudioBASS.GetPeaks(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    Log.Error(exception, "Couldn't check hit sound file");
                    yield return new Issue(GetTemplate("Unable to check"), null, hsFile);

                    continue;
                }

                if (length == 0 || length == BlankHitSoundSize)
                    continue;

                var maxPeak = 0f;

                foreach (var channels in peaks)
                foreach (var level in channels)
                {
                    var abs = Math.Abs(level);

                    if (abs > maxPeak)
                        maxPeak = abs;
                }

                if (maxPeak < SilenceThreshold)
                    yield return new Issue(GetTemplate("Silent"), null, hsFile);
            }
        }
    }
}
