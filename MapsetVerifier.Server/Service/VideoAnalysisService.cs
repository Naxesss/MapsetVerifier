using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model.VideoAnalysis;
using Serilog;

namespace MapsetVerifier.Server.Service;

/// <summary>
/// Service for video analysis, surfacing the container details of the videos a beatmap set uses and
/// how they line up with the song. The compliance issues mirror the video checks, so the overview
/// and the check results never disagree.
/// </summary>
public static class VideoAnalysisService
{
    private const int MaxAllowedWidth = 1280;
    private const int MaxAllowedHeight = 720;

    /// <summary>
    /// Analyzes every video referenced by the beatmap set, both by its difficulties and its
    /// storyboard.
    /// </summary>
    public static VideoAnalysisResult AnalyzeVideos(string beatmapSetFolder)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);

            if (beatmapSet.Beatmaps.Count == 0)
                return VideoAnalysisResult.CreateError("No beatmaps found in folder.");

            var references = CollectReferences(beatmapSet);
            var complianceIssues = new List<string>();

            var videos = references
                .Select(reference => AnalyzeVideo(beatmapSet, reference))
                .ToList();

            AddSetWideIssues(beatmapSet, videos, complianceIssues);

            return VideoAnalysisResult.CreateSuccess(videos, complianceIssues);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze videos for {Folder}", beatmapSetFolder);

            return VideoAnalysisResult.CreateError($"Analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves the absolute path of a video referenced by the beatmap set, or null when the given
    /// file is not one of them. Used to keep the streaming endpoint from serving arbitrary files.
    /// </summary>
    public static string? ResolveReferencedVideoPath(string beatmapSetFolder, string fileName)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);

        var isReferenced = CollectReferences(beatmapSet)
            .Any(reference =>
                string.Equals(reference.Path, fileName, StringComparison.OrdinalIgnoreCase)
            );

        if (!isReferenced)
            return null;

        var fullPath = Path.GetFullPath(Path.Combine(beatmapSet.SongPath, fileName));
        var folderPath = Path.GetFullPath(beatmapSet.SongPath);

        // Guards against a reference that escapes the beatmap set folder, which the "Leaves Folder"
        // check also flags as a problem.
        if (
            !fullPath.StartsWith(
                folderPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return null;

        return File.Exists(fullPath) ? fullPath : null;
    }

    /// <summary>
    /// Collects every distinct video of the set, keeping track of which difficulties use it.
    /// </summary>
    private static List<VideoReference> CollectReferences(BeatmapSet beatmapSet)
    {
        var references = new Dictionary<string, VideoReference>(StringComparer.OrdinalIgnoreCase);

        foreach (var beatmap in beatmapSet.Beatmaps)
        {
            foreach (var video in beatmap.Videos)
            {
                if (video.path == null)
                    continue;

                if (!references.TryGetValue(video.path, out var reference))
                {
                    reference = new VideoReference(video.path);
                    references[video.path] = reference;
                }

                reference.Offsets.Add(video.offset);
                reference.UsedByDifficulties.Add(beatmap.MetadataSettings.version);
            }
        }

        if (beatmapSet.Osb != null)
        {
            foreach (var video in beatmapSet.Osb.videos)
            {
                if (video.path == null)
                    continue;

                if (!references.TryGetValue(video.path, out var reference))
                {
                    reference = new VideoReference(video.path);
                    references[video.path] = reference;
                }

                reference.Offsets.Add(video.offset);
                reference.UsedByDifficulties.Add("(Storyboard)");
            }
        }

        return references.Values.ToList();
    }

    private static VideoAnalysisEntry AnalyzeVideo(BeatmapSet beatmapSet, VideoReference reference)
    {
        var entry = new VideoAnalysisEntry
        {
            FileName = reference.Path,
            OffsetMs = reference.Offsets.FirstOrDefault(),
            UsedByDifficulties = reference.UsedByDifficulties.ToList(),
        };

        var fullPath = Path.Combine(beatmapSet.SongPath, reference.Path);

        if (!File.Exists(fullPath))
        {
            entry.Exists = false;
            entry.BadgeType = "error";
            entry.ComplianceIssues.Add(
                "This video is referenced but not present in the folder, so it could not be checked. Make sure you downloaded the mapset with video."
            );

            return entry;
        }

        entry.Exists = true;

        var metadata = VideoMetadataReader.Read(fullPath);

        entry.FileSizeBytes = metadata.FileSizeBytes;
        entry.FileSizeFormatted = FileSizeFormatter.Format(metadata.FileSizeBytes);
        entry.Container = metadata.Container;
        entry.VideoCodec = metadata.VideoCodec;
        entry.VideoCodecProfile = metadata.VideoCodecProfile;
        entry.Width = metadata.Width;
        entry.Height = metadata.Height;
        entry.Resolution = $"{metadata.Width} × {metadata.Height}";
        // Deriving the frame rate from sample counts leaves floating point noise behind.
        entry.FrameRate = metadata.FrameRate.HasValue
            ? Math.Round(metadata.FrameRate.Value, 3)
            : null;
        entry.IsVariableFrameRate = metadata.IsVariableFrameRate;
        entry.VideoBitrateKbps = metadata.VideoBitrateBps / 1000.0;
        entry.OverallBitrateKbps = metadata.OverallBitrateBps / 1000.0;
        entry.HasAudioTrack = metadata.HasAudioTrack;
        entry.AudioCodec = metadata.AudioCodec;
        entry.AudioChannels = metadata.AudioChannels;
        entry.AudioSampleRate = metadata.AudioSampleRate;
        entry.DurationMs = metadata.DurationMs;
        entry.DurationFormatted = DurationFormatter.Format(metadata.DurationMs);
        entry.Warnings = metadata.Warnings.ToList();
        entry.CanPreview = CanBrowserPlay(metadata);

        AddVideoIssues(entry);

        return entry;
    }

    /// <summary>
    /// Mirrors the resolution and audio track checks, which are the ones that apply per video.
    /// </summary>
    private static void AddVideoIssues(VideoAnalysisEntry entry)
    {
        if (entry.Width > MaxAllowedWidth || entry.Height > MaxAllowedHeight)
            entry.ComplianceIssues.Add(
                $"Resolution is greater than {MaxAllowedWidth} x {MaxAllowedHeight} ({entry.Width} x {entry.Height})."
            );

        if (entry.HasAudioTrack)
            entry.ComplianceIssues.Add(
                "An audio track is present, which is never played but still takes up file size."
            );

        entry.IsCompliant = entry.ComplianceIssues.Count == 0;

        if (!entry.IsCompliant)
            entry.BadgeType = "error";
        else if (entry.Warnings.Count > 0 || entry.Width == 0)
            entry.BadgeType = "warning";
    }

    /// <summary>
    /// Mirrors the checks that look at the set as a whole rather than a single file.
    /// </summary>
    private static void AddSetWideIssues(
        BeatmapSet beatmapSet,
        List<VideoAnalysisEntry> videos,
        List<string> complianceIssues
    )
    {
        if (videos.Count > 1)
            complianceIssues.Add(
                $"The set uses {videos.Count} different videos, where only one is expected."
            );

        var offsets = beatmapSet
            .Beatmaps.Where(beatmap => beatmap.Videos.Count > 0)
            .Select(beatmap => beatmap.Videos[0].offset)
            .Distinct()
            .ToList();

        if (offsets.Count > 1)
            complianceIssues.Add(
                $"Difficulties use inconsistent video offsets ({string.Join(", ", offsets.Order())} ms)."
            );
    }

    /// <summary>
    /// Whether the preview player can be expected to play the file, which is limited to what
    /// Chromium supports.
    /// </summary>
    private static bool CanBrowserPlay(VideoMetadata metadata)
    {
        if (metadata.Container is not ("MP4" or "WEBM"))
            return false;

        return metadata.VideoCodec is "H.264 / AVC" or "H.265 / HEVC" or "VP8" or "VP9" or "AV1";
    }

    private sealed class VideoReference(string path)
    {
        public string Path { get; } = path;
        public List<int> Offsets { get; } = [];
        public List<string> UsedByDifficulties { get; } = [];
    }
}
