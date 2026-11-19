using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.RankingCriteria;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.General.Audio;

[Check]
public class CheckSampleRate : GeneralCheck
{
    private const int MaxSampleRate = 48000;

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Audio",
            Message = "Too high audio sample rate.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Keeping the sample rate of the song at 48 kHz or lower."
                },
                {
                    "Reasoning",
                    @"
                    Sample rates above 48 kHz only add frequencies nobody can hear, while making the file larger. Most songs are
                    released at 44.1 kHz or 48 kHz, so a higher sample rate usually means the file was converted or exported
                    with the wrong settings."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "SampleRate",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" has a sample rate of {1} kHz, which is higher than 48 kHz.",
                    "path",
                    "sample rate"
                )
                    .WithCause("The song has a sample rate higher than 48 kHz.")
                    .WithRule(RC.General.Audio_SamplingRateNoLarger48)
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
        var audioPath = beatmapSet.GetAudioFilePath();

        if (audioPath == null)
            yield break;

        var relativePath = PathStatic.RelativePath(audioPath, beatmapSet.SongPath);
        int sampleRate;

        try
        {
            sampleRate = AudioFileCache.GetSampleRate(audioPath);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Couldn't read the sample rate of {Path}", audioPath);
            sampleRate = -1;
        }

        if (sampleRate < 0)
        {
            yield return new Issue(GetTemplate("Exception"), null, relativePath);
            yield break;
        }

        if (sampleRate > MaxSampleRate)
            yield return new Issue(
                GetTemplate("SampleRate"),
                null,
                relativePath,
                $"{sampleRate / 1000d:0.###}"
            );
    }
}
