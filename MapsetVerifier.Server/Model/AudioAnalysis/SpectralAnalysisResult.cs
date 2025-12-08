namespace MapsetVerifier.Server.Model.AudioAnalysis;

/// <summary>
/// Result of spectral/spectrogram analysis.
/// </summary>
public readonly struct SpectralAnalysisResult
{
    /// <summary>
    /// Spectrogram data as a 2D array [time][frequency] with dB values.
    /// </summary>
    public IEnumerable<SpectrogramFrame> SpectrogramData { get; init; }
    
    /// <summary>
    /// Frequency bins used in the analysis (in Hz).
    /// </summary>
    public IEnumerable<double> FrequencyBins { get; init; }
    
    /// <summary>
    /// Time positions for each frame (in ms).
    /// </summary>
    public IEnumerable<double> TimePositions { get; init; }
    
    /// <summary>
    /// Minimum dB value in the spectrogram.
    /// </summary>
    public double MinDb { get; init; }
    
    /// <summary>
    /// Maximum dB value in the spectrogram.
    /// </summary>
    public double MaxDb { get; init; }
    
    /// <summary>
    /// FFT window size used.
    /// </summary>
    public int FftSize { get; init; }
    
    /// <summary>
    /// Sample rate of the analyzed audio.
    /// </summary>
    public int SampleRate { get; init; }
    
    /// <summary>
    /// Peak frequencies detected with annotations.
    /// </summary>
    public IEnumerable<PeakFrequency> PeakFrequencies { get; init; }
}

/// <summary>
/// A single frame of spectrogram data.
/// </summary>
public readonly struct SpectrogramFrame
{
    /// <summary>
    /// Time position in milliseconds.
    /// </summary>
    public double TimeMs { get; init; }
    
    /// <summary>
    /// Magnitude values for each frequency bin (in dB).
    /// </summary>
    public IEnumerable<float> Magnitudes { get; init; }
}

/// <summary>
/// A detected peak frequency.
/// </summary>
public readonly struct PeakFrequency
{
    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public double FrequencyHz { get; init; }
    
    /// <summary>
    /// Magnitude in dB.
    /// </summary>
    public double MagnitudeDb { get; init; }
    
    /// <summary>
    /// Time position where peak was detected (in ms).
    /// </summary>
    public double TimeMs { get; init; }
    
    /// <summary>
    /// Musical note name (e.g., "A4", "C#5").
    /// </summary>
    public string NoteName { get; init; }
    
    /// <summary>
    /// Cents deviation from the exact note frequency.
    /// </summary>
    public double CentsDeviation { get; init; }
}
