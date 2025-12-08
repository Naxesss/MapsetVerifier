namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of dynamic range analysis.
/// </summary>
public readonly struct DynamicRangeResult
{
    /// <summary>
    /// Loudness Range (LRA) in LU (Loudness Units) per EBU R128.
    /// </summary>
    public double LoudnessRange { get; init; }
    
    /// <summary>
    /// Integrated loudness in LUFS.
    /// </summary>
    public double IntegratedLoudness { get; init; }
    
    /// <summary>
    /// True peak level in dBTP.
    /// </summary>
    public double TruePeak { get; init; }
    
    /// <summary>
    /// Peak level in dBFS.
    /// </summary>
    public double PeakLevel { get; init; }
    
    /// <summary>
    /// RMS (Root Mean Square) level in dBFS.
    /// </summary>
    public double RmsLevel { get; init; }
    
    /// <summary>
    /// Dynamic range (Peak - RMS) in dB.
    /// </summary>
    public double DynamicRange { get; init; }
    
    /// <summary>
    /// Crest factor (Peak / RMS ratio).
    /// </summary>
    public double CrestFactor { get; init; }
    
    /// <summary>
    /// Whether dynamic range compression is detected.
    /// </summary>
    public bool CompressionDetected { get; init; }
    
    /// <summary>
    /// Compression severity (None, Light, Moderate, Heavy).
    /// </summary>
    public CompressionSeverity CompressionSeverity { get; init; }
    
    /// <summary>
    /// Whether audio clipping is detected.
    /// </summary>
    public bool ClippingDetected { get; init; }
    
    /// <summary>
    /// Number of clipping occurrences.
    /// </summary>
    public int ClippingCount { get; init; }
    
    /// <summary>
    /// Timestamps where clipping occurs (in ms).
    /// </summary>
    public IEnumerable<ClippingMarker> ClippingMarkers { get; init; }
    
    /// <summary>
    /// Loudness data over time for visualization.
    /// </summary>
    public IEnumerable<LoudnessDataPoint> LoudnessOverTime { get; init; }
}

/// <summary>
/// Compression severity levels.
/// </summary>
public enum CompressionSeverity
{
    /// <summary>No significant compression detected.</summary>
    None = 0,
    /// <summary>Light compression, natural dynamics preserved.</summary>
    Light = 1,
    /// <summary>Moderate compression, some dynamics lost.</summary>
    Moderate = 2,
    /// <summary>Heavy compression, significant dynamics loss.</summary>
    Heavy = 3
}

/// <summary>
/// A marker for a clipping occurrence.
/// </summary>
public readonly struct ClippingMarker
{
    /// <summary>
    /// Time position in milliseconds.
    /// </summary>
    public double TimeMs { get; init; }
    
    /// <summary>
    /// Duration of the clipping in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
    
    /// <summary>
    /// Peak level during clipping.
    /// </summary>
    public double PeakLevel { get; init; }
    
    /// <summary>
    /// Which channel(s) are clipping (Left, Right, Both).
    /// </summary>
    public string Channel { get; init; }
}

/// <summary>
/// A single data point for loudness over time.
/// </summary>
public readonly struct LoudnessDataPoint
{
    /// <summary>
    /// Time position in milliseconds.
    /// </summary>
    public double TimeMs { get; init; }
    
    /// <summary>
    /// Momentary loudness in LUFS.
    /// </summary>
    public double MomentaryLoudness { get; init; }
    
    /// <summary>
    /// Short-term loudness in LUFS.
    /// </summary>
    public double ShortTermLoudness { get; init; }
    
    /// <summary>
    /// Peak level at this time in dBFS.
    /// </summary>
    public double PeakLevel { get; init; }
    
    /// <summary>
    /// RMS level at this time in dBFS.
    /// </summary>
    public double RmsLevel { get; init; }
}

