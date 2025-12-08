namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Request model for audio analysis endpoints.
/// </summary>
public class AudioAnalysisRequest
{
    /// <summary>
    /// The folder path of the beatmap set to analyze.
    /// </summary>
    public string BeatmapSetFolder { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Specific audio file path relative to the beatmap folder.
    /// If not provided, the main audio file will be analyzed.
    /// </summary>
    public string? AudioFile { get; set; }
}

/// <summary>
/// Request model for hit sound batch analysis.
/// </summary>
public class HitSoundAnalysisRequest
{
    /// <summary>
    /// The folder path of the beatmap set to analyze.
    /// </summary>
    public string BeatmapSetFolder { get; set; } = string.Empty;
}

/// <summary>
/// Request model for spectrogram generation with configurable parameters.
/// </summary>
public class SpectrogramRequest
{
    /// <summary>
    /// The folder path of the beatmap set.
    /// </summary>
    public string BeatmapSetFolder { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Specific audio file path relative to the beatmap folder.
    /// </summary>
    public string? AudioFile { get; set; }

    /// <summary>
    /// FFT window size (power of 2). Default is 4096.
    /// </summary>
    public int FftSize { get; set; } = 4096;

    /// <summary>
    /// Time resolution in milliseconds. Default is 10.
    /// </summary>
    public int TimeResolutionMs { get; set; } = 10;
}

/// <summary>
/// Request model for frequency analysis with FFT data.
/// </summary>
public class FrequencyAnalysisRequest
{
    /// <summary>
    /// The folder path of the beatmap set.
    /// </summary>
    public string BeatmapSetFolder { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Specific audio file path relative to the beatmap folder.
    /// </summary>
    public string? AudioFile { get; set; }

    /// <summary>
    /// FFT window size (power of 2). Default is 4096.
    /// </summary>
    public int FftSize { get; set; } = 4096;
}

