namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of channel balance and stereo analysis.
/// </summary>
public readonly struct ChannelAnalysisResult
{
    /// <summary>
    /// Number of audio channels (1 = mono, 2 = stereo).
    /// </summary>
    public int ChannelCount { get; init; }
    
    /// <summary>
    /// Whether the audio is mono.
    /// </summary>
    public bool IsMono { get; init; }
    
    /// <summary>
    /// Whether the audio is stereo.
    /// </summary>
    public bool IsStereo { get; init; }
    
    /// <summary>
    /// Left channel average level (0.0 to 1.0).
    /// </summary>
    public double LeftChannelLevel { get; init; }
    
    /// <summary>
    /// Right channel average level (0.0 to 1.0).
    /// </summary>
    public double RightChannelLevel { get; init; }
    
    /// <summary>
    /// Channel balance ratio. 1.0 = perfectly balanced.
    /// Values > 1.0 indicate left is louder, < 1.0 indicate right is louder.
    /// </summary>
    public double BalanceRatio { get; init; }
    
    /// <summary>
    /// Imbalance severity level (None, Minor, Warning, Severe).
    /// </summary>
    public ImbalanceSeverity Severity { get; init; }
    
    /// <summary>
    /// Which channel is louder (Left, Right, or Balanced).
    /// </summary>
    public string LouderChannel { get; init; }
    
    /// <summary>
    /// Phase correlation value (-1.0 to 1.0).
    /// 1.0 = mono compatible, 0.0 = unrelated, -1.0 = out of phase.
    /// </summary>
    public double PhaseCorrelation { get; init; }
    
    /// <summary>
    /// Stereo width measurement (0.0 = mono, 1.0 = full stereo).
    /// </summary>
    public double StereoWidth { get; init; }
    
    /// <summary>
    /// Channel balance data over time for visualization.
    /// </summary>
    public IEnumerable<ChannelBalanceDataPoint> BalanceOverTime { get; init; }
}

/// <summary>
/// Severity levels for channel imbalance.
/// </summary>
public enum ImbalanceSeverity
{
    /// <summary>No imbalance detected.</summary>
    None = 0,
    /// <summary>Minor imbalance, not a significant issue.</summary>
    Minor = 1,
    /// <summary>Notable imbalance that may be jarring.</summary>
    Warning = 2,
    /// <summary>Severe imbalance, one channel is silent or nearly silent.</summary>
    Severe = 3
}

/// <summary>
/// A single data point for channel balance over time.
/// </summary>
public readonly struct ChannelBalanceDataPoint
{
    /// <summary>
    /// Time position in milliseconds.
    /// </summary>
    public double TimeMs { get; init; }
    
    /// <summary>
    /// Left channel level at this time (0.0 to 1.0).
    /// </summary>
    public float LeftLevel { get; init; }
    
    /// <summary>
    /// Right channel level at this time (0.0 to 1.0).
    /// </summary>
    public float RightLevel { get; init; }
    
    /// <summary>
    /// Balance value at this time (-1.0 = full left, 0.0 = center, 1.0 = full right).
    /// </summary>
    public double Balance { get; init; }
}

