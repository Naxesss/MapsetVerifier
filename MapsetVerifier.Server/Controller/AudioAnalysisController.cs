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
                return BadRequest(new ApiError("Folder is required.", null));

            var result = AudioAnalysisService.AnalyzeAudio(
                request.BeatmapSetFolder,
                request.AudioFile
            );

            if (!result.Success)
                return NotFound(
                    new ApiError(result.ErrorMessage ?? "Audio analysis failed.", null)
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze audio for {Folder}", request.BeatmapSetFolder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred during audio analysis.")
            );
        }
    }

    /// <summary>
    /// Streams the audio of a beatmap set so it can be played in the client.
    /// </summary>
    [HttpGet("stream")]
    public ActionResult StreamAudio([FromQuery] string folder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder))
                return BadRequest(new ApiError("Folder is required.", null));

            var fullPath = AudioAnalysisService.ResolveAudioStreamPath(folder);

            if (fullPath == null)
                return NotFound(new ApiError("Audio file not found in beatmap set.", null));

            return PhysicalFile(fullPath, GetMimeType(fullPath), enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stream audio from {Folder}", folder);

            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred streaming the audio.")
            );
        }
    }

    private static string GetMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream",
        };

    /// <summary>
    /// Gets spectrogram data for visualization.
    /// </summary>
    [HttpPost("spectrogram")]
    public ActionResult<SpectralAnalysisResult> GetSpectrogram(
        [FromBody] SpectrogramRequest request
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null));

            var result = AudioAnalysisService.GetSpectralAnalysis(
                request.BeatmapSetFolder,
                request.AudioFile,
                request.FftSize,
                request.TimeResolutionMs
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate spectrogram for {Folder}", request.BeatmapSetFolder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred generating spectrogram.")
            );
        }
    }

    /// <summary>
    /// Gets frequency analysis with FFT data and harmonic analysis.
    /// </summary>
    [HttpPost("frequency")]
    public ActionResult<FrequencyAnalysisResult> GetFrequencyAnalysis(
        [FromBody] FrequencyAnalysisRequest request
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null));

            var result = AudioAnalysisService.GetFrequencyAnalysis(
                request.BeatmapSetFolder,
                request.AudioFile,
                request.FftSize
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze frequency for {Folder}", request.BeatmapSetFolder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred during frequency analysis.")
            );
        }
    }
}
