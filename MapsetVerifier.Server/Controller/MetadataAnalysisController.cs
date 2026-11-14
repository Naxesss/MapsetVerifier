using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Model.MetadataAnalysis;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

/// <summary>
/// Controller for metadata analysis endpoints providing beatmap metadata, resources, and colour settings.
/// </summary>
[ApiController]
[Route("metadata")]
public class MetadataAnalysisController : ControllerBase
{
    /// <summary>
    /// Performs complete metadata analysis on a beatmap set including metadata, resources, and colour settings.
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<MetadataAnalysisResult> AnalyzeMetadata(
        [FromBody] MetadataAnalysisRequest request
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null));

            var result = MetadataAnalysisService.Analyze(request.BeatmapSetFolder);

            if (!result.Success)
                return NotFound(
                    new ApiError(result.ErrorMessage ?? "Metadata analysis failed.", null)
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze metadata for {Folder}", request.BeatmapSetFolder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred during metadata analysis.")
            );
        }
    }

    /// <summary>
    /// Serves a background or hit sound of a beatmap set, so the client can show or play it. Only
    /// files the set uses are served, and only from within its folder.
    /// </summary>
    [HttpGet("file")]
    public ActionResult GetResourceFile([FromQuery] string folder, [FromQuery] string file)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(file))
                return BadRequest(new ApiError("Folder and file are required.", null));

            var fullPath = MetadataAnalysisService.ResolveResourceFilePath(folder, file);

            if (fullPath == null)
                return NotFound(new ApiError("File not found in beatmap set.", null));

            Response.Headers.CacheControl = "private, max-age=3600";

            return PhysicalFile(fullPath, GetMimeType(fullPath));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to serve {File} from {Folder}", file, folder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred serving the file.")
            );
        }
    }

    private static string GetMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream",
        };
}
