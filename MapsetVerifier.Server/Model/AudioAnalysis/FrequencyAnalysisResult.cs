namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of frequency analysis including FFT and harmonic analysis.
/// </summary>
public readonly struct FrequencyAnalysisResult
{
    /// <summary>
    /// FFT data for the entire audio (averaged).
    /// </summary>
    public IEnumerable<FftDataPoint> FftData { get; init; }
    
    /// <summary>
    /// FFT window size used.
    /// </summary>
    public int FftWindowSize { get; init; }
    
    /// <summary>
    /// Detected peak frequencies mapped to musical notes.
    /// </summary>
    public IEnumerable<DetectedNote> DetectedNotes { get; init; }
    
    /// <summary>
    /// Harmonic analysis results.
    /// </summary>
    public HarmonicAnalysis HarmonicAnalysis { get; init; }
    
    /// <summary>
    /// Frequency masking detection results.
    /// </summary>
    public IEnumerable<FrequencyMaskingResult> MaskingResults { get; init; }
    
    /// <summary>
    /// Dominant frequency in Hz.
    /// </summary>
    public double DominantFrequency { get; init; }
    
    /// <summary>
    /// Fundamental frequency estimate in Hz.
    /// </summary>
    public double FundamentalFrequency { get; init; }
}

/// <summary>
/// A single FFT data point.
/// </summary>
public readonly struct FftDataPoint
{
    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public double FrequencyHz { get; init; }
    
    /// <summary>
    /// Magnitude in dB.
    /// </summary>
    public double MagnitudeDb { get; init; }
}

/// <summary>
/// A detected musical note from frequency analysis.
/// </summary>
public readonly struct DetectedNote
{
    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public double FrequencyHz { get; init; }
    
    /// <summary>
    /// Note name (e.g., "A4", "C#5").
    /// </summary>
    public string NoteName { get; init; }
    
    /// <summary>
    /// MIDI note number.
    /// </summary>
    public int MidiNote { get; init; }
    
    /// <summary>
    /// Cents deviation from exact pitch.
    /// </summary>
    public double CentsDeviation { get; init; }
    
    /// <summary>
    /// Confidence level (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Harmonic analysis results.
/// </summary>
public readonly struct HarmonicAnalysis
{
    /// <summary>
    /// Detected harmonics with their frequencies and magnitudes.
    /// </summary>
    public IEnumerable<Harmonic> Harmonics { get; init; }
    
    /// <summary>
    /// Estimated fundamental frequency.
    /// </summary>
    public double FundamentalHz { get; init; }
    
    /// <summary>
    /// Harmonic-to-noise ratio in dB.
    /// </summary>
    public double HarmonicToNoiseRatio { get; init; }
    
    /// <summary>
    /// Bass frequency range energy (20-250 Hz).
    /// </summary>
    public double BassEnergy { get; init; }
    
    /// <summary>
    /// Mid frequency range energy (250-4000 Hz).
    /// </summary>
    public double MidEnergy { get; init; }
    
    /// <summary>
    /// High frequency range energy (4000-20000 Hz).
    /// </summary>
    public double HighEnergy { get; init; }
}

/// <summary>
/// A single harmonic component.
/// </summary>
public readonly struct Harmonic
{
    /// <summary>
    /// Harmonic number (1 = fundamental, 2 = first overtone, etc.).
    /// </summary>
    public int HarmonicNumber { get; init; }
    
    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public double FrequencyHz { get; init; }
    
    /// <summary>
    /// Magnitude in dB.
    /// </summary>
    public double MagnitudeDb { get; init; }
}

/// <summary>
/// Result of frequency masking detection.
/// </summary>
public readonly struct FrequencyMaskingResult
{
    /// <summary>
    /// Center frequency of the masked region in Hz.
    /// </summary>
    public double CenterFrequencyHz { get; init; }
    
    /// <summary>
    /// Bandwidth of the masked region in Hz.
    /// </summary>
    public double BandwidthHz { get; init; }
    
    /// <summary>
    /// Severity of masking (0.0 to 1.0).
    /// </summary>
    public double Severity { get; init; }
    
    /// <summary>
    /// Description of the masking issue.
    /// </summary>
    public string Description { get; init; }
}

