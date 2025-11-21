using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("beatmaps")]
public class BeatmapsController : ControllerBase
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
            songsFolder = BeatmapsService.DetectSongsFolder();
        }
        if (string.IsNullOrWhiteSpace(songsFolder))
            return NotFound(new ApiError("Songs folder could not be detected.", null));

        if (page < 0) page = 0;
        if (pageSize <= 0) pageSize = 16;
        if (pageSize > 100) pageSize = 100; // safety cap

        var pageResult = BeatmapsService.GetBeatmaps(songsFolder, search, page, pageSize);
        if (!pageResult.Items.Any())
            return NotFound(new ApiError(search != null ? "The search yielded no results." : "No mapsets could be found in the Songs folder.", null));
        return Ok(pageResult);
    }

    [HttpGet("songsFolder")]
    public ActionResult GetSongsFolder()
    {
        var folder = BeatmapsService.DetectSongsFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return NotFound(new ApiError("Songs folder could not be detected.", null));
        return Ok(new { songsFolder = folder });
    }

    [HttpGet("image")]
    public ActionResult<FileStreamResult> GetBeatmapImage([FromQuery] string folder, [FromQuery] bool original = false)
    {
        var result = BeatmapsService.GetBeatmapImage(folder, original);
        if (!result.Success)
            return NotFound(new ApiError(result.ErrorMessage ?? "Image not found", null));

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
}
