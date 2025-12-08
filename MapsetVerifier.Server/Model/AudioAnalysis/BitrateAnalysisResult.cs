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
    
    /// <summary>
    /// Bitrate samples over time for visualization (time in ms, bitrate in kbps).
    /// </summary>
    public IEnumerable<BitrateDataPoint> BitrateOverTime { get; init; }
    
    /// <summary>
    /// Whether the bitrate is within the acceptable range (128-208 kbps).
    /// </summary>
    public bool IsCompliant { get; init; }
    
    /// <summary>
    /// Compliance status message.
    /// </summary>
    public string ComplianceMessage { get; init; }
    
    /// <summary>
    /// Format-specific maximum bitrate (192 for MP3, 208 for OGG).
    /// </summary>
    public int MaxAllowedBitrate { get; init; }
    
    /// <summary>
    /// Minimum allowed bitrate (128 kbps).
    /// </summary>
    public int MinAllowedBitrate { get; init; }
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

/// <summary>
/// Result of hit sound batch validation.
/// </summary>
public readonly struct HitSoundBatchResult
{
    /// <summary>
    /// Total number of hit sound files analyzed.
    /// </summary>
    public int TotalFiles { get; init; }
    
    /// <summary>
    /// Number of compliant hit sound files.
    /// </summary>
    public int CompliantFiles { get; init; }
    
    /// <summary>
    /// Number of non-compliant hit sound files.
    /// </summary>
    public int NonCompliantFiles { get; init; }
    
    /// <summary>
    /// Individual results for each hit sound file.
    /// </summary>
    public IEnumerable<HitSoundAnalysisResult> Results { get; init; }
}

/// <summary>
/// Analysis result for a single hit sound file.
/// </summary>
public readonly struct HitSoundAnalysisResult
{
    /// <summary>
    /// Relative file path of the hit sound.
    /// </summary>
    public string FilePath { get; init; }
    
    /// <summary>
    /// Audio format (MP3, OGG, WAV).
    /// </summary>
    public string Format { get; init; }
    
    /// <summary>
    /// Bitrate in kbps.
    /// </summary>
    public double Bitrate { get; init; }
    
    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
    
    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public int Channels { get; init; }
    
    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; init; }
    
    /// <summary>
    /// Whether the hit sound is compliant with ranking criteria.
    /// </summary>
    public bool IsCompliant { get; init; }
    
    /// <summary>
    /// List of compliance issues if any.
    /// </summary>
    public IEnumerable<string> Issues { get; init; }
    
    /// <summary>
    /// Channel balance ratio (1.0 = balanced, >2.0 = imbalanced).
    /// </summary>
    public double ChannelBalanceRatio { get; init; }
    
    /// <summary>
    /// Whether the hit sound has channel imbalance issues.
    /// </summary>
    public bool HasImbalance { get; init; }
}

