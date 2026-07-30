namespace MapsetVerifier.Server.Model.VideoAnalysis;

/// <summary>
/// Complete video analysis result for every video referenced by a beatmap set.
/// </summary>
public class VideoAnalysisResult
{
    /// <summary>
    /// Whether the analysis was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Every video referenced by the beatmap set, including ones whose file is missing.
    /// </summary>
    public List<VideoAnalysisEntry> Videos { get; set; } = [];

    /// <summary>
    /// Compliance issues that apply to the set as a whole rather than a single video.
    /// </summary>
    public List<string> ComplianceIssues { get; set; } = [];

    /// <summary>
    /// Whether every video is compliant and no set-wide issues were found.
    /// </summary>
    public bool IsCompliant =>
        ComplianceIssues.Count == 0 && Videos.All(video => video.IsCompliant);

    public static VideoAnalysisResult CreateError(string message) =>
        new() { Success = false, ErrorMessage = message };

    public static VideoAnalysisResult CreateSuccess(
        List<VideoAnalysisEntry> videos,
        List<string> complianceIssues
    ) =>
        new()
        {
            Success = true,
            Videos = videos,
            ComplianceIssues = complianceIssues,
        };
}

/// <summary>
/// Everything known about a single video file of a beatmap set.
/// </summary>
public class VideoAnalysisEntry
{
    /// <summary>
    /// File name as referenced by the beatmaps, relative to the beatmap set folder.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the referenced file is actually present in the beatmap set folder.
    /// </summary>
    public bool Exists { get; set; }

    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Container name, e.g. "MP4".
    /// </summary>
    public string Container { get; set; } = string.Empty;

    public string? VideoCodec { get; set; }
    public string? VideoCodecProfile { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// Average frame rate, or null when the container does not let us derive it.
    /// </summary>
    public double? FrameRate { get; set; }

    public bool IsVariableFrameRate { get; set; }

    /// <summary>
    /// Bitrate of the video track alone in kbps, or null when unavailable.
    /// </summary>
    public double? VideoBitrateKbps { get; set; }

    /// <summary>
    /// Bitrate of the whole file in kbps.
    /// </summary>
    public double OverallBitrateKbps { get; set; }

    public bool HasAudioTrack { get; set; }
    public string? AudioCodec { get; set; }
    public int AudioChannels { get; set; }
    public int AudioSampleRate { get; set; }

    public double DurationMs { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Offset in milliseconds at which the video starts playing, from the .osu file.
    /// </summary>
    public int OffsetMs { get; set; }

    /// <summary>
    /// Difficulty names using this video, or "(Storyboard)" when it comes from the .osb.
    /// </summary>
    public List<string> UsedByDifficulties { get; set; } = [];

    public bool IsCompliant { get; set; }
    public List<string> ComplianceIssues { get; set; } = [];

    /// <summary>
    /// Badge colour hint for the client, one of "success", "warning" or "error".
    /// </summary>
    public string BadgeType { get; set; } = "success";

    /// <summary>
    /// Whether the browser can be expected to play this file in the preview player.
    /// </summary>
    public bool CanPreview { get; set; }

    /// <summary>
    /// Notes about fields that could not be determined for this file.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
