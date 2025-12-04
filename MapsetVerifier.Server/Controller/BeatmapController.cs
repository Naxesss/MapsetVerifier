using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("beatmap")]
public class BeatmapController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiBeatmapPage> GetBeatmaps(
        [FromQuery] string? songsFolder,
        [FromQuery] string? search,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 16)
    {
        if (string.IsNullOrWhiteSpace(songsFolder))
        {
            songsFolder = BeatmapService.DetectSongsFolder();
        }
        if (string.IsNullOrWhiteSpace(songsFolder))
            return NotFound(new ApiError("Songs folder could not be detected.", null, null));

        if (page < 0) page = 0;
        if (pageSize <= 0) pageSize = 16;
        if (pageSize > 100) pageSize = 100; // safety cap

        var pageResult = BeatmapService.GetBeatmaps(songsFolder, search, page, pageSize);

        // If we have items, always 200.
        if (pageResult.Items.Any())
            return Ok(pageResult);

        // No items: only 404 when first page and no more folders.
        if (page == 0 && !pageResult.HasMore)
            return NotFound(new ApiError(search != null ? "The search yielded no results." : "No mapsets could be found in the Songs folder.", null, null));

        // Empty page beyond available results (or intermediate) -> still 200 with empty payload.
        return Ok(pageResult);
    }

    [HttpGet("songsFolder")]
    public ActionResult GetSongsFolder()
    {
        var folder = BeatmapService.DetectSongsFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return NotFound(new ApiError("Songs folder could not be detected.", null, null));
        return Ok(new { songsFolder = folder });
    }

    [HttpGet("image")]
    public ActionResult GetBeatmapImage([FromQuery] string folder, [FromQuery] bool original = false)
    {
        var result = BeatmapService.GetBeatmapImage(folder, original);
        if (!result.Success)
            return NotFound(new ApiError(result.ErrorMessage ?? "Image not found", null, null));

        Response.Headers.CacheControl = "public, max-age=86400";
        if (!string.IsNullOrWhiteSpace(result.ETag))
        {
            Response.Headers.ETag = result.ETag;
            if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm == result.ETag)
                return StatusCode(304);
        }

        if (result.DataStream != null)
        {
            // Return the in-memory resized image
            return File(result.DataStream, result.MimeType!);
        }

        var stream = System.IO.File.OpenRead(result.ImagePath!);
        return File(stream, result.MimeType!);
    }
    
    [HttpPost("runChecks")]
    public ActionResult<ApiBeatmapSetCheckResult> RunBeatmapSetChecks([FromBody] RunChecksRequest request)
    {
        try
        {
            var result = BeatmapService.RunBeatmapSetChecks(request.Folder);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run beatmap checks for {Folder}", request.Folder);
            return StatusCode(500, new ApiError("An error occurred while running beatmap checks.", ex.Message, ex.StackTrace));
        }
    }

    [HttpPost("runCheck/override")]
    public ActionResult<ApiCategoryOverrideCheckResult> RunCheckWithOverride([FromBody] RunCheckOverrideRequest request)
    {
        try
        {
            var result = BeatmapService.RunDifficultyCheckWithOverride(
                request.Folder,
                request.DifficultyName,
                request.OverrideDifficulty);

            if (result == null)
                return NotFound(new ApiError($"Difficulty '{request.DifficultyName}' not found in beatmap set.", null, null));

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiError("An error occurred while running beatmap check with override.", ex.Message, ex.StackTrace));
        }
    }
}
