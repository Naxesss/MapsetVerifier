using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Model.VideoAnalysis;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

/// <summary>
/// Controller for video analysis endpoints, providing the container details of the videos a beatmap
/// set uses along with the file itself for previewing.
/// </summary>
[ApiController]
[Route("video")]
public class VideoAnalysisController : ControllerBase
{
    /// <summary>
    /// Analyzes every video referenced by a beatmap set.
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<VideoAnalysisResult> AnalyzeVideos([FromBody] VideoAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BeatmapSetFolder))
                return BadRequest(new ApiError("Folder is required.", null));

            var result = VideoAnalysisService.AnalyzeVideos(request.BeatmapSetFolder);

            if (!result.Success)
                return NotFound(
                    new ApiError(result.ErrorMessage ?? "Video analysis failed.", null)
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to analyze videos for {Folder}", request.BeatmapSetFolder);

            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred during video analysis.")
            );
        }
    }

    /// <summary>
    /// Streams a video of a beatmap set so it can be played in the client. Only files actually
    /// referenced by the set are served, and only from within its folder.
    /// </summary>
    [HttpGet("stream")]
    public ActionResult StreamVideo([FromQuery] string folder, [FromQuery] string file)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(file))
                return BadRequest(new ApiError("Folder and file are required.", null));

            var fullPath = VideoAnalysisService.ResolveReferencedVideoPath(folder, file);

            if (fullPath == null)
                return NotFound(new ApiError("Video not found in beatmap set.", null));

            var stream = System.IO.File.OpenRead(fullPath);

            Response.Headers.CacheControl = "private, max-age=3600";

            return File(stream, GetMimeType(fullPath), enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stream video {File} from {Folder}", file, folder);

            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred streaming the video.")
            );
        }
    }

    /// <summary>
    /// Beatmap sets fairly often carry an MP4 under an .avi name, so the container the file's own
    /// bytes report wins over its extension. Getting this wrong stops the browser from playing a
    /// file it otherwise supports.
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        var container = VideoMetadataReader.SniffContainer(filePath);

        if (container != null)
            return container switch
            {
                "MP4" => "video/mp4",
                "AVI" => "video/x-msvideo",
                "FLV" => "video/x-flv",
                _ => "application/octet-stream",
            };

        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".flv" => "video/x-flv",
            ".wmv" => "video/x-ms-wmv",
            ".mpg" or ".mpeg" => "video/mpeg",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream",
        };
    }
}
