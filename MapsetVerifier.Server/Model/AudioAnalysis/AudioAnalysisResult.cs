namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Complete audio analysis result combining all analysis types.
/// </summary>
public readonly struct AudioAnalysisResult
{
    /// <summary>
    /// Whether the analysis was successful.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Path to the analyzed audio file.
    /// </summary>
    public string AudioFilePath { get; init; }
    
    /// <summary>
    /// Bitrate analysis results.
    /// </summary>
    public BitrateAnalysisResult? BitrateAnalysis { get; init; }
    
    /// <summary>
    /// Channel balance and stereo analysis results.
    /// </summary>
    public ChannelAnalysisResult? ChannelAnalysis { get; init; }
    
    /// <summary>
    /// Audio format compliance results.
    /// </summary>
    public FormatAnalysisResult? FormatAnalysis { get; init; }
    
    /// <summary>
    /// Dynamic range analysis results.
    /// </summary>
    public DynamicRangeResult? DynamicRangeAnalysis { get; init; }
    
    /// <summary>
    /// Overall compliance status for ranking criteria.
    /// </summary>
    public bool IsCompliant { get; init; }
    
    /// <summary>
    /// List of all compliance issues found.
    /// </summary>
    public IEnumerable<string> ComplianceIssues { get; init; }
    
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AudioAnalysisResult CreateSuccess(
        string audioFilePath,
        BitrateAnalysisResult bitrateAnalysis,
        ChannelAnalysisResult channelAnalysis,
        FormatAnalysisResult formatAnalysis,
        DynamicRangeResult dynamicRangeAnalysis,
        IEnumerable<string> complianceIssues)
    {
        return new AudioAnalysisResult
        {
            Success = true,
            ErrorMessage = null,
            AudioFilePath = audioFilePath,
            BitrateAnalysis = bitrateAnalysis,
            ChannelAnalysis = channelAnalysis,
            FormatAnalysis = formatAnalysis,
            DynamicRangeAnalysis = dynamicRangeAnalysis,
            IsCompliant = !complianceIssues.Any(),
            ComplianceIssues = complianceIssues
        };
    }
    
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AudioAnalysisResult CreateError(string audioFilePath, string errorMessage)
    {
        return new AudioAnalysisResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            AudioFilePath = audioFilePath,
            BitrateAnalysis = null,
            ChannelAnalysis = null,
            FormatAnalysis = null,
            DynamicRangeAnalysis = null,
            IsCompliant = false,
            ComplianceIssues = [errorMessage]
        };
    }
}

