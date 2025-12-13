using ManagedBass;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using MapsetVerifier.Server.Model.AudioAnalysis;
using Serilog;

namespace MapsetVerifier.Server.Service;

/// <summary>
/// Service for comprehensive audio analysis including bitrate, channel balance, format compliance,
/// spectral analysis, and dynamic range.
/// </summary>
public static class AudioAnalysisService
{
    private const int MinAllowedBitrate = 128;
    private const int MaxAllowedBitrateMp3 = 192;
    private const int MaxAllowedBitrateOgg = 208;
    private const int StandardSampleRate = 44100;
    private const int MaxAllowedSampleRate = 48000;

    /// <summary>
    /// Performs complete audio analysis on the main audio file of a beatmap set.
    /// </summary>
    public static AudioAnalysisResult AnalyzeAudio(string beatmapSetFolder, string? specificAudioFile = null)
    {
        try
        {
            var beatmapSet = new BeatmapSet(beatmapSetFolder);
            var audioPath = specificAudioFile != null
                ? Path.Combine(beatmapSetFolder, specificAudioFile)
                : beatmapSet.GetAudioFilePath();

            if (audioPath == null || !File.Exists(audioPath))
            {
                return AudioAnalysisResult.CreateError(
                    specificAudioFile ?? "unknown",
                    "Audio file not found in beatmap set.");
            }

            var complianceIssues = new List<string>();

            var bitrateAnalysis = AnalyzeBitrate(audioPath, ref complianceIssues);
            var channelAnalysis = AnalyzeChannels(audioPath);
            var formatAnalysis = AnalyzeFormat(audioPath, ref complianceIssues);
            var dynamicRangeAnalysis = AnalyzeDynamicRange(audioPath);

            return AudioAnalysisResult.CreateSuccess(
                PathStatic.RelativePath(audioPath, beatmapSetFolder),
                bitrateAnalysis,
                channelAnalysis,
                formatAnalysis,
                dynamicRangeAnalysis,
                complianceIssues);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze audio for {Folder}", beatmapSetFolder);
            return AudioAnalysisResult.CreateError(
                specificAudioFile ?? "unknown",
                $"Analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyzes bitrate and VBR characteristics.
    /// </summary>
    private static BitrateAnalysisResult AnalyzeBitrate(string audioPath, ref List<string> complianceIssues)
    {
        var format = AudioBASS.GetFormat(audioPath);
        var bitrate = Math.Round(AudioBASS.GetBitrate(audioPath));
        var duration = AudioBASS.GetDuration(audioPath);

        var formatName = GetFormatName(format);
        var maxAllowed = (format & ChannelType.OGG) != 0 ? MaxAllowedBitrateOgg : MaxAllowedBitrateMp3;
        var isCompliant = bitrate >= MinAllowedBitrate && bitrate <= maxAllowed;

        string complianceMessage;
        if (!isCompliant)
        {
            if (bitrate < MinAllowedBitrate)
            {
                var issue = $"Bitrate {bitrate} kbps is below minimum {MinAllowedBitrate} kbps (recommended for ranking)";
                complianceIssues.Add(issue);
                complianceMessage = $"Bitrate too low: {bitrate} kbps < {MinAllowedBitrate} kbps minimum. Use at least {MinAllowedBitrate} kbps if source quality allows.";
            }
            else
            {
                var issue = $"Bitrate {bitrate} kbps exceeds maximum {maxAllowed} kbps for {formatName}";
                complianceIssues.Add(issue);
                complianceMessage = $"Bitrate too high: {bitrate} kbps > {maxAllowed} kbps maximum for {formatName}. Re-encode to comply with ranking criteria.";
            }
        }
        else
        {
            complianceMessage = $"Bitrate is within acceptable range ({MinAllowedBitrate}-{maxAllowed} kbps for {formatName})";
        }

        // Generate bitrate over time data (simplified - BASS reports average bitrate)
        var bitrateOverTime = GenerateBitrateOverTime(duration, bitrate);

        return new BitrateAnalysisResult
        {
            AverageBitrate = bitrate,
            IsVbr = false, // BASS doesn't easily expose VBR detection
            MinBitrate = null,
            MaxBitrate = null,
            BitrateOverTime = bitrateOverTime,
            IsCompliant = isCompliant,
            ComplianceMessage = complianceMessage,
            MaxAllowedBitrate = maxAllowed,
            MinAllowedBitrate = MinAllowedBitrate
        };
    }

    /// <summary>
    /// Analyzes channel balance and stereo characteristics.
    /// </summary>
    private static ChannelAnalysisResult AnalyzeChannels(string audioPath)
    {
        var channels = AudioBASS.GetChannels(audioPath);
        var peaks = AudioBASS.GetPeaks(audioPath);

        var isMono = channels == 1;
        var isStereo = channels >= 2;

        float leftSum = 0, rightSum = 0;
        var balanceOverTime = new List<ChannelBalanceDataPoint>();

        for (var i = 0; i < peaks.Count; i++)
        {
            var peak = peaks[i];
            var left = peak[0];
            var right = channels > 1 ? peak[1] : peak[0];

            leftSum += left;
            rightSum += right;

            // Sample every 100ms for visualization
            if (i % 100 == 0)
            {
                var balance = (left + right) > 0 ? (right - left) / (left + right) : 0;
                balanceOverTime.Add(new ChannelBalanceDataPoint
                {
                    TimeMs = i,
                    LeftLevel = left,
                    RightLevel = right,
                    Balance = balance
                });
            }
        }

        var leftAvg = peaks.Count > 0 ? leftSum / peaks.Count : 0;
        var rightAvg = peaks.Count > 0 ? rightSum / peaks.Count : 0;

        double balanceRatio = 1.0;
        var louderChannel = "Balanced";
        if (leftSum > 0 && rightSum > 0)
        {
            balanceRatio = leftSum > rightSum ? leftSum / rightSum : rightSum / leftSum;
            louderChannel = Math.Abs(leftSum - rightSum) < 0.01 ? "Balanced" : (leftSum > rightSum ? "Left" : "Right");
        }
        else if (leftSum > 0)
        {
            louderChannel = "Left (Right silent)";
            balanceRatio = double.MaxValue;
        }
        else if (rightSum > 0)
        {
            louderChannel = "Right (Left silent)";
            balanceRatio = double.MaxValue;
        }

        var severity = GetImbalanceSeverity(balanceRatio, leftSum, rightSum);

        // Calculate phase correlation and stereo width (simplified)
        var phaseCorrelation = CalculatePhaseCorrelation(peaks, channels);
        var stereoWidth = isMono ? 0.0 : CalculateStereoWidth(peaks, channels);

        return new ChannelAnalysisResult
        {
            ChannelCount = channels,
            IsMono = isMono,
            IsStereo = isStereo,
            LeftChannelLevel = leftAvg,
            RightChannelLevel = rightAvg,
            BalanceRatio = balanceRatio,
            Severity = severity,
            LouderChannel = louderChannel,
            PhaseCorrelation = phaseCorrelation,
            StereoWidth = stereoWidth,
            BalanceOverTime = balanceOverTime
        };
    }

    /// <summary>
    /// Analyzes audio format and compliance.
    /// </summary>
    private static FormatAnalysisResult AnalyzeFormat(string audioPath, ref List<string> complianceIssues)
    {
        var format = AudioBASS.GetFormat(audioPath);
        var info = AudioAnalyzer.GetAudioInfo(audioPath);
        var fileInfo = new FileInfo(audioPath);

        var formatName = GetFormatName(format);
        var isStandardSampleRate = info.SampleRate == StandardSampleRate;
        var bitDepth = info.IsFloatingPoint ? 32 : 16; // Simplified detection

        var issues = new List<string>();

        // Check if format is MP3 or Ogg Vorbis (required for ranking)
        var isValidFormat = (format & ChannelType.MP3) != 0 || (format & ChannelType.OGG) != 0;
        if (!isValidFormat)
        {
            issues.Add($"Audio format must be MP3 (.mp3) or Ogg Vorbis (.ogg), found: {formatName}");
            complianceIssues.Add($"Invalid audio format: {formatName}. Must be MP3 or Ogg Vorbis");
        }

        // Check sample rate compliance (must not exceed 48 kHz)
        if (info.SampleRate > MaxAllowedSampleRate)
        {
            issues.Add($"Sample rate {info.SampleRate} Hz exceeds maximum allowed {MaxAllowedSampleRate} Hz");
            complianceIssues.Add($"Sample rate {info.SampleRate} Hz exceeds maximum {MaxAllowedSampleRate} Hz");
        }

        var badgeType = issues.Count == 0 ? "success" : "warning";
        if (formatName == "Unknown" || !isValidFormat) badgeType = "error";

        return new FormatAnalysisResult
        {
            Format = formatName,
            RawFormat = AudioBASS.EnumToString(format),
            SampleRate = info.SampleRate,
            IsStandardSampleRate = isStandardSampleRate,
            BitDepth = bitDepth,
            Codec = GetCodecInfo(format),
            DurationMs = info.DurationMs,
            DurationFormatted = FormatDuration(info.DurationMs),
            FileSizeBytes = fileInfo.Length,
            FileSizeFormatted = FormatFileSize(fileInfo.Length),
            Channels = info.Channels,
            IsCompliant = issues.Count == 0,
            ComplianceIssues = issues,
            BadgeType = badgeType
        };
    }

    /// <summary>
    /// Analyzes dynamic range, loudness, and clipping.
    /// </summary>
    private static DynamicRangeResult AnalyzeDynamicRange(string audioPath)
    {
        var levelInfo = AudioAnalyzer.GetLevelInfo(audioPath);
        var channelLevels = AudioAnalyzer.GetChannelLevelsOverTime(audioPath, 100);

        var peakLevel = Math.Max(levelInfo.PeakLeft, levelInfo.PeakRight);
        var rmsLevel = (levelInfo.RmsLeft + levelInfo.RmsRight) / 2;

        // Convert to dB
        var peakDb = peakLevel > 0 ? 20 * Math.Log10(peakLevel) : -100;
        var rmsDb = rmsLevel > 0 ? 20 * Math.Log10(rmsLevel) : -100;
        var dynamicRange = peakDb - rmsDb;
        var crestFactor = rmsLevel > 0 ? peakLevel / rmsLevel : 0;

        // Estimate compression severity based on dynamic range
        var compressionSeverity = GetCompressionSeverity(dynamicRange);
        var compressionDetected = compressionSeverity != CompressionSeverity.None;

        // Build clipping markers
        var clippingMarkers = levelInfo.ClippingTimestamps.Select(t => new ClippingMarker
        {
            TimeMs = t,
            DurationMs = 10, // Approximate
            PeakLevel = 1.0,
            Channel = "Both"
        }).ToList();

        // Build loudness over time data
        var loudnessOverTime = channelLevels.Select(s => new LoudnessDataPoint
        {
            TimeMs = s.TimeMs,
            MomentaryLoudness = CalculateLufs(s.LeftLevel, s.RightLevel),
            ShortTermLoudness = CalculateLufs(s.LeftLevel, s.RightLevel),
            PeakLevel = 20 * Math.Log10(Math.Max(s.LeftLevel, s.RightLevel) + 0.0001),
            RmsLevel = 20 * Math.Log10((s.LeftLevel + s.RightLevel) / 2 + 0.0001)
        }).ToList();

        return new DynamicRangeResult
        {
            LoudnessRange = dynamicRange, // Simplified LRA approximation
            IntegratedLoudness = rmsDb - 10, // Rough LUFS approximation
            TruePeak = peakDb,
            PeakLevel = peakDb,
            RmsLevel = rmsDb,
            DynamicRange = dynamicRange,
            CrestFactor = crestFactor,
            CompressionDetected = compressionDetected,
            CompressionSeverity = compressionSeverity,
            ClippingDetected = levelInfo.ClippingCount > 0,
            ClippingCount = levelInfo.ClippingCount,
            ClippingMarkers = clippingMarkers,
            LoudnessOverTime = loudnessOverTime
        };
    }

    /// <summary>
    /// Performs batch analysis on all hit sounds in a beatmap set.
    /// </summary>
    public static HitSoundBatchResult AnalyzeHitSounds(string beatmapSetFolder)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var results = new List<HitSoundAnalysisResult>();

        foreach (var hsFile in beatmapSet.HitSoundFiles)
        {
            var hsPath = Path.Combine(beatmapSet.SongPath, hsFile);
            results.Add(AnalyzeSingleHitSound(hsPath, hsFile));
        }

        return new HitSoundBatchResult
        {
            TotalFiles = results.Count,
            CompliantFiles = results.Count(r => r.IsCompliant),
            NonCompliantFiles = results.Count(r => !r.IsCompliant),
            Results = results
        };
    }

    private static HitSoundAnalysisResult AnalyzeSingleHitSound(string hsPath, string relativePath)
    {
        try
        {
            var format = AudioBASS.GetFormat(hsPath);
            var bitrate = Math.Round(AudioBASS.GetBitrate(hsPath));
            var duration = AudioBASS.GetDuration(hsPath);
            var channels = AudioBASS.GetChannels(hsPath);
            var info = AudioAnalyzer.GetAudioInfo(hsPath);

            var issues = new List<string>();
            var hasImbalance = false;
            double balanceRatio = 1.0;

            // Check format compliance (MP3 or Ogg Vorbis)
            var isValidFormat = (format & ChannelType.MP3) != 0 || (format & ChannelType.OGG) != 0;
            if (!isValidFormat)
            {
                issues.Add($"Invalid format: {GetFormatName(format)}. Must be MP3 or Ogg Vorbis");
            }

            // Check sample rate compliance
            if (info.SampleRate > MaxAllowedSampleRate)
            {
                issues.Add($"Sample rate {info.SampleRate} Hz exceeds maximum {MaxAllowedSampleRate} Hz");
            }

            // Check bitrate for compressed formats
            if (isValidFormat && bitrate < MinAllowedBitrate)
            {
                issues.Add($"Bitrate {bitrate} kbps is below minimum {MinAllowedBitrate} kbps");
            }
            else if (isValidFormat)
            {
                var maxAllowed = (format & ChannelType.OGG) != 0 ? MaxAllowedBitrateOgg : MaxAllowedBitrateMp3;
                if (bitrate > maxAllowed)
                {
                    issues.Add($"Bitrate {bitrate} kbps exceeds maximum {maxAllowed} kbps for {GetFormatName(format)}");
                }
            }

            // Check channel imbalance
            if (channels >= 2)
            {
                var peaks = AudioBASS.GetPeaks(hsPath);
                if (peaks.Count > 0)
                {
                    var leftSum = peaks.Sum(p => p[0]);
                    var rightSum = peaks.Sum(p => p.Length > 1 ? p[1] : 0);

                    if (leftSum > 0 && rightSum > 0)
                    {
                        balanceRatio = leftSum > rightSum ? leftSum / rightSum : rightSum / leftSum;
                        if (balanceRatio >= 2)
                        {
                            hasImbalance = true;
                            var louder = leftSum > rightSum ? "left" : "right";
                            issues.Add($"Notable channel imbalance: {louder} channel is louder");
                        }
                    }
                    else if (leftSum == 0 || rightSum == 0)
                    {
                        hasImbalance = true;
                        issues.Add("One channel is completely silent");
                    }
                }
            }

            return new HitSoundAnalysisResult
            {
                FilePath = relativePath,
                Format = GetFormatName(format),
                Bitrate = bitrate,
                DurationMs = duration,
                Channels = channels,
                SampleRate = info.SampleRate,
                IsCompliant = issues.Count == 0,
                Issues = issues,
                ChannelBalanceRatio = balanceRatio,
                HasImbalance = hasImbalance
            };
        }
        catch (Exception ex)
        {
            return new HitSoundAnalysisResult
            {
                FilePath = relativePath,
                Format = "Error",
                Bitrate = 0,
                DurationMs = 0,
                Channels = 0,
                SampleRate = 0,
                IsCompliant = false,
                Issues = [$"Failed to analyze: {ex.Message}"],
                ChannelBalanceRatio = 1.0,
                HasImbalance = false
            };
        }
    }

    /// <summary>
    /// Gets spectrogram data for visualization.
    /// </summary>
    public static SpectralAnalysisResult GetSpectralAnalysis(string beatmapSetFolder, string? specificAudioFile = null,
        int fftSize = 4096, int timeResolutionMs = 10)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var audioPath = specificAudioFile != null
            ? Path.Combine(beatmapSetFolder, specificAudioFile)
            : beatmapSet.GetAudioFilePath();

        if (audioPath == null || !File.Exists(audioPath))
        {
            return new SpectralAnalysisResult
            {
                SpectrogramData = [],
                FrequencyBins = [],
                TimePositions = [],
                MinDb = -100,
                MaxDb = 0,
                FftSize = fftSize,
                SampleRate = 0,
                PeakFrequencies = []
            };
        }

        var spectrogramData = AudioAnalyzer.GetSpectrogramData(audioPath, fftSize, timeResolutionMs);

        // Calculate frequency range dynamically using Nyquist frequency
        const int minFreq = 20; // Minimum audible frequency
        var maxFreq = spectrogramData.SampleRate / 2; // Nyquist frequency

        // Filter frequency bins to valid range
        var filteredBins = spectrogramData.FrequencyBins
            .Select((freq, idx) => new { Freq = freq, Idx = idx })
            .Where(x => x.Freq >= minFreq && x.Freq <= maxFreq)
            .ToList();

        var minIdx = filteredBins.FirstOrDefault()?.Idx ?? 0;
        var maxIdx = filteredBins.LastOrDefault()?.Idx ?? spectrogramData.FrequencyBins.Length - 1;

        var frames = spectrogramData.Frames.Select(f => new Model.AudioAnalysis.SpectrogramFrame
        {
            TimeMs = f.TimeMs,
            Magnitudes = f.Magnitudes.Skip(minIdx).Take(maxIdx - minIdx + 1)
        }).ToList();

        var allMagnitudes = frames.SelectMany(f => f.Magnitudes).ToList();
        var minDb = allMagnitudes.Count > 0 ? allMagnitudes.Min() : -100;
        var maxDb = allMagnitudes.Count > 0 ? allMagnitudes.Max() : 0;

        // Detect peak frequencies
        var peakFrequencies = DetectPeakFrequencies(spectrogramData, minFreq, maxFreq);

        return new SpectralAnalysisResult
        {
            SpectrogramData = frames,
            FrequencyBins = filteredBins.Select(x => x.Freq),
            TimePositions = frames.Select(f => f.TimeMs),
            MinDb = minDb,
            MaxDb = maxDb,
            FftSize = fftSize,
            SampleRate = spectrogramData.SampleRate,
            PeakFrequencies = peakFrequencies
        };
    }

    /// <summary>
    /// Gets frequency analysis with FFT data and harmonic analysis.
    /// </summary>
    public static FrequencyAnalysisResult GetFrequencyAnalysis(string beatmapSetFolder, string? specificAudioFile = null,
        int fftSize = 4096)
    {
        var beatmapSet = new BeatmapSet(beatmapSetFolder);
        var audioPath = specificAudioFile != null
            ? Path.Combine(beatmapSetFolder, specificAudioFile)
            : beatmapSet.GetAudioFilePath();

        if (audioPath == null || !File.Exists(audioPath))
        {
            return new FrequencyAnalysisResult
            {
                FftData = [],
                FftWindowSize = fftSize,
                DetectedNotes = [],
                HarmonicAnalysis = new HarmonicAnalysis(),
                MaskingResults = [],
                DominantFrequency = 0,
                FundamentalFrequency = 0
            };
        }

        var fftData = AudioAnalyzer.GetFftData(audioPath, GetDataFlags(fftSize));
        var info = AudioAnalyzer.GetAudioInfo(audioPath);

        var binWidth = (double)info.SampleRate / fftSize;
        var fftPoints = fftData.Select((mag, idx) => new FftDataPoint
        {
            FrequencyHz = idx * binWidth,
            MagnitudeDb = mag > 0 ? 20 * Math.Log10(mag) : -100
        }).Where(p => p.FrequencyHz <= 24000).ToList();

        // Find dominant frequency
        var dominantPoint = fftPoints.OrderByDescending(p => p.MagnitudeDb).FirstOrDefault();
        var dominantFreq = dominantPoint.FrequencyHz;

        // Detect notes from peaks
        var detectedNotes = DetectNotesFromFft(fftPoints);

        // Harmonic analysis
        var harmonicAnalysis = AnalyzeHarmonics(fftPoints, dominantFreq);

        return new FrequencyAnalysisResult
        {
            FftData = fftPoints,
            FftWindowSize = fftSize,
            DetectedNotes = detectedNotes,
            HarmonicAnalysis = harmonicAnalysis,
            MaskingResults = DetectFrequencyMasking(fftPoints),
            DominantFrequency = dominantFreq,
            FundamentalFrequency = harmonicAnalysis.FundamentalHz
        };
    }

    #region Helper Methods

    private static IEnumerable<BitrateDataPoint> GenerateBitrateOverTime(double durationMs, double avgBitrate)
    {
        var points = new List<BitrateDataPoint>();
        var interval = Math.Max(1000, durationMs / 100); // Max 100 points
        for (double t = 0; t < durationMs; t += interval)
        {
            points.Add(new BitrateDataPoint { TimeMs = t, Bitrate = avgBitrate });
        }
        return points;
    }

    private static ImbalanceSeverity GetImbalanceSeverity(double balanceRatio, float leftSum, float rightSum)
    {
        if (leftSum == 0 || rightSum == 0)
            return ImbalanceSeverity.Severe;
        if (balanceRatio >= 2)
            return ImbalanceSeverity.Warning;
        if (balanceRatio >= 1.5)
            return ImbalanceSeverity.Minor;
        return ImbalanceSeverity.None;
    }

    private static double CalculatePhaseCorrelation(List<float[]> peaks, int channels)
    {
        if (channels < 2 || peaks.Count == 0) return 1.0;

        double sumProduct = 0, sumLeftSq = 0, sumRightSq = 0;
        foreach (var peak in peaks)
        {
            var left = peak[0];
            var right = peak.Length > 1 ? peak[1] : peak[0];
            sumProduct += left * right;
            sumLeftSq += left * left;
            sumRightSq += right * right;
        }

        var denominator = Math.Sqrt(sumLeftSq * sumRightSq);
        return denominator > 0 ? sumProduct / denominator : 1.0;
    }

    private static double CalculateStereoWidth(List<float[]> peaks, int channels)
    {
        if (channels < 2 || peaks.Count == 0) return 0.0;

        double sumDiff = 0, sumTotal = 0;
        foreach (var peak in peaks)
        {
            var left = peak[0];
            var right = peak.Length > 1 ? peak[1] : peak[0];
            sumDiff += Math.Abs(left - right);
            sumTotal += left + right;
        }

        return sumTotal > 0 ? sumDiff / sumTotal : 0.0;
    }

    private static string GetFormatName(ChannelType format)
    {
        if ((format & ChannelType.MP3) != 0) return "MP3";
        if ((format & ChannelType.OGG) != 0) return "OGG";
        if ((format & ChannelType.Wave) != 0) return "WAV";
        if ((format & ChannelType.AIFF) != 0) return "AIFF";
        if ((format & ChannelType.FLAC) != 0) return "FLAC";
        return "Unknown";
    }

    private static string GetCodecInfo(ChannelType format)
    {
        if ((format & ChannelType.MP3) != 0) return "MPEG Audio Layer III";
        if ((format & ChannelType.OGG) != 0) return "Ogg Vorbis";
        if ((format & ChannelType.Wave) != 0) return "PCM Wave";
        if ((format & ChannelType.AIFF) != 0) return "Audio Interchange File Format";
        if ((format & ChannelType.FLAC) != 0) return "Free Lossless Audio Codec";
        return "Unknown Codec";
    }

    private static string FormatDuration(double durationMs)
    {
        var ts = TimeSpan.FromMilliseconds(durationMs);
        return ts.Hours > 0
            ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private static CompressionSeverity GetCompressionSeverity(double dynamicRange)
    {
        if (dynamicRange >= 14) return CompressionSeverity.None;
        if (dynamicRange >= 10) return CompressionSeverity.Light;
        if (dynamicRange >= 6) return CompressionSeverity.Moderate;
        return CompressionSeverity.Heavy;
    }

    private static double CalculateLufs(float left, float right)
    {
        var rms = Math.Sqrt((left * left + right * right) / 2);
        return rms > 0 ? -0.691 + 10 * Math.Log10(rms * rms) : -70;
    }

    private static DataFlags GetDataFlags(int fftSize)
    {
        return fftSize switch
        {
            256 => DataFlags.FFT256,
            512 => DataFlags.FFT512,
            1024 => DataFlags.FFT1024,
            2048 => DataFlags.FFT2048,
            4096 => DataFlags.FFT4096,
            8192 => DataFlags.FFT8192,
            16384 => DataFlags.FFT16384,
            _ => DataFlags.FFT4096
        };
    }


    private static IEnumerable<PeakFrequency> DetectPeakFrequencies(SpectrogramData data, int minFreq, int maxFreq)
    {
        var peaks = new List<PeakFrequency>();
        if (data.Frames.Count == 0) return peaks;

        // Average magnitudes across all frames
        var avgMagnitudes = new float[data.FrequencyBins.Length];
        foreach (var frame in data.Frames)
        {
            for (var i = 0; i < Math.Min(frame.Magnitudes.Length, avgMagnitudes.Length); i++)
            {
                avgMagnitudes[i] += frame.Magnitudes[i];
            }
        }
        for (var i = 0; i < avgMagnitudes.Length; i++)
        {
            avgMagnitudes[i] /= data.Frames.Count;
        }

        // Find local maxima
        for (var i = 1; i < avgMagnitudes.Length - 1; i++)
        {
            var freq = data.FrequencyBins[i];
            if (freq < minFreq || freq > maxFreq) continue;

            if (avgMagnitudes[i] > avgMagnitudes[i - 1] && avgMagnitudes[i] > avgMagnitudes[i + 1] && avgMagnitudes[i] > -60)
            {
                var (noteName, cents) = FrequencyToNote(freq);
                peaks.Add(new PeakFrequency
                {
                    FrequencyHz = freq,
                    MagnitudeDb = avgMagnitudes[i],
                    TimeMs = 0,
                    NoteName = noteName,
                    CentsDeviation = cents
                });
            }
        }

        return peaks.OrderByDescending(p => p.MagnitudeDb).Take(20);
    }

    private static List<DetectedNote> DetectNotesFromFft(List<FftDataPoint> fftPoints)
    {
        var notes = new List<DetectedNote>();
        var threshold = fftPoints.Max(p => p.MagnitudeDb) - 20;

        var peaks = fftPoints
            .Where(p => p.MagnitudeDb > threshold && p.FrequencyHz is > 20 and < 5000)
            .OrderByDescending(p => p.MagnitudeDb)
            .Take(10);

        foreach (var peak in peaks)
        {
            var (noteName, cents) = FrequencyToNote(peak.FrequencyHz);
            var midiNote = FrequencyToMidi(peak.FrequencyHz);
            notes.Add(new DetectedNote
            {
                FrequencyHz = peak.FrequencyHz,
                NoteName = noteName,
                MidiNote = midiNote,
                CentsDeviation = cents,
                Confidence = (peak.MagnitudeDb - threshold) / 20
            });
        }

        return notes;
    }

    private static HarmonicAnalysis AnalyzeHarmonics(List<FftDataPoint> fftPoints, double fundamentalFreq)
    {
        var harmonics = new List<Harmonic>();

        if (fundamentalFreq > 0)
        {
            for (var h = 1; h <= 8; h++)
            {
                var targetFreq = fundamentalFreq * h;
                var closest = fftPoints.OrderBy(p => Math.Abs(p.FrequencyHz - targetFreq)).FirstOrDefault();
                if (Math.Abs(closest.FrequencyHz - targetFreq) < targetFreq * 0.05)
                {
                    harmonics.Add(new Harmonic
                    {
                        HarmonicNumber = h,
                        FrequencyHz = closest.FrequencyHz,
                        MagnitudeDb = closest.MagnitudeDb
                    });
                }
            }
        }

        // Calculate energy in frequency bands
        var bassEnergy = fftPoints.Where(p => p.FrequencyHz is >= 20 and < 250).Average(p => p.MagnitudeDb);
        var midEnergy = fftPoints.Where(p => p.FrequencyHz is >= 250 and < 4000).Average(p => p.MagnitudeDb);
        var highEnergy = fftPoints.Where(p => p.FrequencyHz is >= 4000 and <= 24000).Average(p => p.MagnitudeDb);

        return new HarmonicAnalysis
        {
            Harmonics = harmonics,
            FundamentalHz = fundamentalFreq,
            HarmonicToNoiseRatio = 0, // Would require more complex analysis
            BassEnergy = bassEnergy,
            MidEnergy = midEnergy,
            HighEnergy = highEnergy
        };
    }

    private static List<FrequencyMaskingResult> DetectFrequencyMasking(List<FftDataPoint> fftPoints)
    {
        var results = new List<FrequencyMaskingResult>();

        // Simple masking detection: look for frequency ranges with high energy density
        var bands = new[] { (20, 250, "Bass"), (250, 2000, "Mids"), (2000, 8000, "High Mids") };

        foreach (var (min, max, name) in bands)
        {
            var bandPoints = fftPoints.Where(p => p.FrequencyHz >= min && p.FrequencyHz < max).ToList();
            if (bandPoints.Count == 0) continue;

            var avgEnergy = bandPoints.Average(p => p.MagnitudeDb);
            var maxEnergy = bandPoints.Max(p => p.MagnitudeDb);

            if (maxEnergy - avgEnergy < 6 && avgEnergy > -40)
            {
                results.Add(new FrequencyMaskingResult
                {
                    CenterFrequencyHz = (min + max) / 2.0,
                    BandwidthHz = max - min,
                    Severity = (maxEnergy - avgEnergy) / 6,
                    Description = $"Potential masking in {name} range ({min}-{max} Hz)"
                });
            }
        }

        return results;
    }

    private static (string NoteName, double Cents) FrequencyToNote(double frequency)
    {
        if (frequency <= 0) return ("N/A", 0);

        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var a4 = 440.0;
        var c0 = a4 * Math.Pow(2, -4.75);

        var halfSteps = 12 * Math.Log2(frequency / c0);
        var octave = (int)(halfSteps / 12);
        var noteIndex = (int)Math.Round(halfSteps) % 12;
        if (noteIndex < 0) noteIndex += 12;

        var exactNote = c0 * Math.Pow(2, Math.Round(halfSteps) / 12.0);
        var cents = 1200 * Math.Log2(frequency / exactNote);

        return ($"{noteNames[noteIndex]}{octave}", cents);
    }

    private static int FrequencyToMidi(double frequency)
    {
        if (frequency <= 0) return 0;
        return (int)Math.Round(69 + 12 * Math.Log2(frequency / 440.0));
    }

    #endregion
}