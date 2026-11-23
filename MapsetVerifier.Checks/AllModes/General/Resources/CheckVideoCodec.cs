using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.RankingCriteria;

namespace MapsetVerifier.Checks.AllModes.General.Resources;

[Check]
public class CheckVideoCodec : GeneralCheck
{
    private const string H264 = "H.264 / AVC";

    public override CheckMetadata GetMetadata() =>
        new()
        {
            Category = "Resources",
            Message = "Video not encoded in H.264.",
            Author = "Greaper",

            Documentation = new Dictionary<string, string>
            {
                {
                    "Purpose",
                    @"
                    Making sure background videos are encoded in H.264."
                },
                {
                    "Reasoning",
                    @"
                    H.264 plays back reliably on every platform osu! runs on and keeps the file size reasonable. Other codecs,
                    such as H.265 or VP9, may not play at all for some players, or play with stutters."
                },
            },
        };

    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "Codec",
                new IssueTemplate(
                    Issue.Level.Problem,
                    "\"{0}\" is encoded in {1} instead of H.264.",
                    "file name",
                    "codec"
                )
                    .WithCause("A video uses a codec other than H.264.")
                    .WithRule(RC.General.VideoAndBackground_VideoEncodedH264)
            },
            {
                "Unknown",
                new IssueTemplate(
                    Issue.Level.Warning,
                    "\"{0}\" could not be checked for its codec" + Common.CHECK_MANUALLY_MESSAGE,
                    "file name"
                )
                    .WithCause("The codec of a video could not be read from the file.")
                    .WithRule(RC.General.VideoAndBackground_VideoEncodedH264)
            },
        };

    public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
    {
        var videoPaths = beatmapSet
            .Beatmaps.SelectMany(beatmap => beatmap.Videos)
            .Concat(beatmapSet.Osb?.videos ?? [])
            .Select(video => video.path)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var videoPath in videoPaths)
        {
            // Missing and out-of-folder videos are reported by the video resolution check.
            if (videoPath.StartsWith(".."))
                continue;

            var fullPath = Path.Combine(beatmapSet.SongPath, videoPath);

            if (!File.Exists(fullPath))
                continue;

            var codec = VideoMetadataReader.Read(fullPath).VideoCodec;

            if (codec == null)
                yield return new Issue(GetTemplate("Unknown"), null, videoPath);
            else if (codec != H264)
                yield return new Issue(GetTemplate("Codec"), null, videoPath, codec);
        }
    }
}
