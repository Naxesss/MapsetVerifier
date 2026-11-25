using MapsetVerifier.Framework.Objects.Resources;
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
    /// Reads what the beatmap set's storyboard is made of: its images, samples and layers.
    /// </summary>
    [HttpPost("storyboard")]
    public ActionResult<StoryboardDetailResult> AnalyzeStoryboard(
        [FromBody] MetadataAnalysisRequest request
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null));

            var result = StoryboardDetailService.Analyze(request.BeatmapSetFolder);

            if (!result.Success)
                return NotFound(
                    new ApiError(result.ErrorMessage ?? "Reading the storyboard failed.", null)
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read the storyboard of {Folder}", request.BeatmapSetFolder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred reading the storyboard.")
            );
        }
    }

    /// <summary>
    /// Serves a background, hit sound or storyboard file of a beatmap set, so the client can show or play it. Only
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
        ImageFileFormat.GetMimeType(ImageFileFormat.FromExtension(filePath))
        ?? Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream",
        };
}
