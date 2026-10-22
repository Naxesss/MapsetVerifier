namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of bitrate analysis for an audio file.
/// </summary>
public readonly struct BitrateAnalysisResult
{
    /// <summary>
    /// Average bitrate in kbps.
    /// </summary>
    public double AverageBitrate { get; init; }

    /// <summary>
    /// Whether the audio uses Variable Bitrate (VBR) encoding.
    /// </summary>
    public bool IsVbr { get; init; }

    /// <summary>
    /// Minimum bitrate detected (for VBR files).
    /// </summary>
    public double? MinBitrate { get; init; }

    /// <summary>
    /// Maximum bitrate detected (for VBR files).
    /// </summary>
    public double? MaxBitrate { get; init; }
}

/// <summary>
/// A single data point for bitrate over time visualization.
/// </summary>
public readonly struct BitrateDataPoint
{
    /// <summary>
    /// Time position in milliseconds.
    /// </summary>
    public double TimeMs { get; init; }

    /// <summary>
    /// Bitrate at this time position in kbps.
    /// </summary>
    public double Bitrate { get; init; }
}
