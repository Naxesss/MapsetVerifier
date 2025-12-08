using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Model.AudioAnalysis;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

/// <summary>
/// Controller for audio analysis endpoints providing comprehensive audio quality metrics.
/// </summary>
[ApiController]
[Route("audio")]
public class AudioAnalysisController : ControllerBase
{
    /// <summary>
    /// Performs complete audio analysis on the main audio file of a beatmap set.
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<AudioAnalysisResult> AnalyzeAudio([FromBody] AudioAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = AudioAnalysisService.AnalyzeAudio(request.BeatmapSetFolder, request.AudioFile);

            if (!result.Success)
                return NotFound(new ApiError(result.ErrorMessage ?? "Audio analysis failed.", null, null));

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze audio for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred during audio analysis.", ex.Message, ex.StackTrace));
        }
    }

    /// <summary>
    /// Gets spectrogram data for visualization.
    /// </summary>
    [HttpPost("spectrogram")]
    public ActionResult<SpectralAnalysisResult> GetSpectrogram([FromBody] SpectrogramRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = AudioAnalysisService.GetSpectralAnalysis(
                request.BeatmapSetFolder,
                request.AudioFile,
                request.FftSize,
                request.TimeResolutionMs);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate spectrogram for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred generating spectrogram.", ex.Message, ex.StackTrace));
        }
    }

    /// <summary>
    /// Gets frequency analysis with FFT data and harmonic analysis.
    /// </summary>
    [HttpPost("frequency")]
    public ActionResult<FrequencyAnalysisResult> GetFrequencyAnalysis([FromBody] FrequencyAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = AudioAnalysisService.GetFrequencyAnalysis(
                request.BeatmapSetFolder,
                request.AudioFile,
                request.FftSize);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze frequency for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred during frequency analysis.", ex.Message, ex.StackTrace));
        }
    }

    /// <summary>
    /// Performs batch analysis on all hit sounds in a beatmap set.
    /// </summary>
    [HttpPost("hitsounds")]
    public ActionResult<HitSoundBatchResult> AnalyzeHitSounds([FromBody] HitSoundAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = AudioAnalysisService.AnalyzeHitSounds(request.BeatmapSetFolder);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze hit sounds for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred during hit sound analysis.", ex.Message, ex.StackTrace));
        }
    }
}

