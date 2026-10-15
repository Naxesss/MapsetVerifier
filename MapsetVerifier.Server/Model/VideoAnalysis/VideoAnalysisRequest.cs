namespace MapsetVerifier.Server.Model.VideoAnalysis;

/// <summary>
/// Request model for video analysis endpoints.
/// </summary>
public class VideoAnalysisRequest
{
    /// <summary>
    /// The folder path of the beatmap set to analyze.
    /// </summary>
    public string BeatmapSetFolder { get; set; } = string.Empty;
}
