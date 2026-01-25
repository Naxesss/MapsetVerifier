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
    public ActionResult<MetadataAnalysisResult> AnalyzeMetadata([FromBody] MetadataAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = MetadataAnalysisService.Analyze(request.BeatmapSetFolder);

            if (!result.Success)
                return NotFound(new ApiError(result.ErrorMessage ?? "Metadata analysis failed.", null, null));

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze metadata for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred during metadata analysis.", ex.Message, ex.StackTrace));
        }
    }
}

