namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of audio format compliance analysis.
/// </summary>
public readonly struct FormatAnalysisResult
{
    /// <summary>
    /// Audio format type (MP3, OGG, WAV, etc.).
    /// </summary>
    public string Format { get; init; }
    
    /// <summary>
    /// Raw format flags from the audio library.
    /// </summary>
    public string RawFormat { get; init; }
    
    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; init; }
    
    /// <summary>
    /// Whether the sample rate is the standard 44.1kHz.
    /// </summary>
    public bool IsStandardSampleRate { get; init; }
    
    /// <summary>
    /// Codec information string.
    /// </summary>
    public string Codec { get; init; }
    
    /// <summary>
    /// Audio duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
    
    /// <summary>
    /// Audio duration formatted as mm:ss.
    /// </summary>
    public string DurationFormatted { get; init; }
    
    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
    
    /// <summary>
    /// File size formatted (e.g., "4.5 MB").
    /// </summary>
    public string FileSizeFormatted { get; init; }
    
    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public int Channels { get; init; }
    
    /// <summary>
    /// Whether the format is acceptable for ranking.
    /// </summary>
    public bool IsCompliant { get; init; }
    
    /// <summary>
    /// List of format compliance issues if any.
    /// </summary>
    public IEnumerable<string> ComplianceIssues { get; init; }
    
    /// <summary>
    /// Visual badge type for the format (success, warning, error).
    /// </summary>
    public string BadgeType { get; init; }
}

