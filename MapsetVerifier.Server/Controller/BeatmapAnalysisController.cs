using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Model.BeatmapAnalysis;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

/// <summary>
/// Controller for beatmap analysis endpoints providing statistics, general settings, and difficulty settings.
/// </summary>
[ApiController]
[Route("beatmap-analysis")]
public class BeatmapAnalysisController : ControllerBase
{
    /// <summary>
    /// Performs complete beatmap analysis on a beatmap set including statistics, general settings, and difficulty settings.
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<BeatmapAnalysisResult> AnalyzeBeatmap([FromBody] BeatmapAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null, null));

            var result = BeatmapAnalysisService.Analyze(request.BeatmapSetFolder);

            if (!result.Success)
                return NotFound(new ApiError(result.ErrorMessage ?? "Beatmap analysis failed.", null, null));

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze beatmap for {Folder}", request.BeatmapSetFolder);
            return StatusCode(500, new ApiError("An error occurred during beatmap analysis.", ex.Message, ex.StackTrace));
        }
    }
}

