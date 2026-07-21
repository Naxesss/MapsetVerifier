using System.Threading.Channels;
using MapsetVerifier.Framework;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using MapsetVerifier.Server.Service.OsuRuntime;
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
        [FromQuery] int pageSize = 16,
        [FromQuery] string? bookmarkedFolders = null
    )
    {
        if (string.IsNullOrWhiteSpace(songsFolder))
        {
            songsFolder = BeatmapService.DetectSongsFolder();
        }
        if (string.IsNullOrWhiteSpace(songsFolder))
            return NotFound(new ApiError("Songs folder could not be detected.", null));

        if (page < 0)
            page = 0;
        if (pageSize <= 0)
            pageSize = 16;
        if (pageSize > 100)
            pageSize = 100; // safety cap

        HashSet<string>? bookmarkedFolderSet = null;
        if (!string.IsNullOrWhiteSpace(bookmarkedFolders))
        {
            bookmarkedFolderSet = bookmarkedFolders
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();
        }

        var pageResult = BeatmapService.GetBeatmaps(
            songsFolder,
            search,
            page,
            pageSize,
            bookmarkedFolderSet
        );

        // If we have items, always 200.
        if (pageResult.Items.Any())
            return Ok(pageResult);

        // No items: only 404 when first page and no more folders.
        if (page == 0 && !pageResult.HasMore)
            return NotFound(
                new ApiError(
                    search != null
                        ? "The search yielded no results."
                        : "No mapsets could be found in the Songs folder.",
                    null
                )
            );

        // Empty page beyond available results (or intermediate) -> still 200 with empty payload.
        return Ok(pageResult);
    }

    [HttpGet("songsFolder")]
    public ActionResult GetSongsFolder()
    {
        var folder = BeatmapService.DetectSongsFolder();
        if (string.IsNullOrWhiteSpace(folder))
            return NotFound(new ApiError("Songs folder could not be detected.", null));
        return Ok(new { songsFolder = folder });
    }

    [HttpGet("lazer")]
    public ActionResult<ApiBeatmapPage> GetLazerBeatmaps(
        [FromQuery] string? search,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 16,
        [FromQuery] string? lazerDataDir = null
    )
    {
        if (page < 0)
            page = 0;
        if (pageSize <= 0)
            pageSize = 16;
        if (pageSize > 100)
            pageSize = 100; // safety cap

        var pageResult = BeatmapService.GetLazerBeatmaps(search, page, pageSize, lazerDataDir);

        if (pageResult.Items.Any())
            return Ok(pageResult);

        if (page == 0 && !pageResult.HasMore)
            return NotFound(
                new ApiError(
                    search != null
                        ? "The search yielded no results."
                        : "No mapsets could be found in the lazer library.",
                    null
                )
            );

        return Ok(pageResult);
    }

    [HttpGet("lazer/dataDir")]
    public ActionResult GetLazerDataDir()
    {
        var folder = LazerRealmService.DetectLazerDataDirectory();
        if (string.IsNullOrWhiteSpace(folder))
            return NotFound(new ApiError("Lazer data folder could not be detected.", null));
        return Ok(new { lazerDataDir = folder });
    }

    [HttpPost("lazer/materialize")]
    public ActionResult<ApiLazerMaterializeResult> MaterializeLazerBeatmap(
        [FromBody] MaterializeLazerBeatmapRequest request
    )
    {
        try
        {
            var result = BeatmapService.MaterializeLazerBeatmap(
                request.BeatmapSetId,
                request.LazerDataDir
            );
            if (!result.Success)
                return NotFound(new ApiError(result.ErrorMessage ?? "Beatmapset not found.", null));

            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Failed to materialize lazer beatmapset {BeatmapSetId}",
                request.BeatmapSetId
            );
            return StatusCode(
                500,
                ApiErrorFactory.FromException(
                    ex,
                    "An error occurred while materializing the lazer beatmapset."
                )
            );
        }
    }

    [HttpGet("lazer/current")]
    public ActionResult<ApiLazerLookupResult> GetCurrentLazerBeatmap()
    {
        var result = BeatmapService.GetCurrentLazerBeatmap();
        return Ok(result);
    }

    [HttpGet("stable/current")]
    public ActionResult<ApiLazerLookupResult> GetCurrentStableBeatmap(
        [FromQuery] string? songsFolder = null
    )
    {
        var result = BeatmapService.GetCurrentStableBeatmap(songsFolder);
        return Ok(result);
    }

    [HttpGet("image")]
    public ActionResult GetBeatmapImage(
        [FromQuery] string folder,
        [FromQuery] bool original = false,
        [FromQuery] string? songsFolder = null
    )
    {
        var result = BeatmapService.GetBeatmapImage(folder, original, songsFolder);
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

    [HttpGet("lazer/image")]
    public ActionResult GetLazerBeatmapImage(
        [FromQuery] string id,
        [FromQuery] bool original = false,
        [FromQuery] string? lazerDataDir = null
    )
    {
        var result = BeatmapService.GetLazerBeatmapImage(id, original, lazerDataDir);
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
            return File(result.DataStream, result.MimeType!);

        var stream = System.IO.File.OpenRead(result.ImagePath!);
        return File(stream, result.MimeType!);
    }

    [HttpPost("info")]
    public ActionResult<ApiBeatmapInfo> GetBeatmapInfo([FromBody] RunChecksRequest request)
    {
        try
        {
            var result = BeatmapService.GetBeatmapInfo(request.Folder);
            if (result == null)
                return NotFound(new ApiError("Beatmap info could not be found.", null));

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get beatmap info for {Folder}", request.Folder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred while getting beatmap info.")
            );
        }
    }

    [HttpPost("runChecks")]
    public ActionResult<ApiBeatmapSetCheckResult> RunBeatmapSetChecks(
        [FromBody] RunChecksRequest request
    )
    {
        try
        {
            var result = BeatmapService.RunBeatmapSetChecks(
                request.Folder,
                includeCheckRunDelta: request.IncludeCheckRunDelta,
                createSnapshot: request.CreateSnapshot
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run beatmap checks for {Folder}", request.Folder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred while running beatmap checks.")
            );
        }
    }

    [HttpPost("runChecks/stream")]
    public async Task RunBeatmapSetChecksStream(
        [FromBody] RunChecksRequest request,
        CancellationToken cancellationToken
    )
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        await Response.StartAsync(cancellationToken);

        BeatmapSet beatmapSet;
        try
        {
            var parsed = BeatmapService.ParseBeatmapSet(request.Folder);
            beatmapSet = parsed.BeatmapSet;
            await SseWriter.WriteEventAsync(
                Response,
                "structure",
                parsed.Structure,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to parse beatmap set for {Folder}", request.Folder);
            await SseWriter.WriteEventAsync(
                Response,
                "error",
                ApiErrorFactory.FromException(
                    ex,
                    "An error occurred while parsing the beatmap set."
                ),
                cancellationToken
            );
            return;
        }

        var channel = Channel.CreateUnbounded<(string EventType, object Data)>();

        var progress = new Progress<CheckProgress>(update =>
            channel.Writer.TryWrite(("progress", update))
        );

        var runTask = Task.Run(
            () =>
            {
                try
                {
                    var result = BeatmapService.RunBeatmapSetChecks(
                        beatmapSet,
                        progress,
                        request.IncludeCheckRunDelta,
                        request.CreateSnapshot
                    );
                    channel.Writer.TryWrite(("complete", result));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to run beatmap checks for {Folder}", request.Folder);
                    channel.Writer.TryWrite(
                        (
                            "error",
                            ApiErrorFactory.FromException(
                                ex,
                                "An error occurred while running beatmap checks."
                            )
                        )
                    );
                }
                finally
                {
                    channel.Writer.Complete();
                }
            },
            cancellationToken
        );

        try
        {
            await foreach (var (eventType, data) in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await SseWriter.WriteEventAsync(Response, eventType, data, cancellationToken);
            }
        }
        finally
        {
            await runTask;
        }
    }

    [HttpPost("runCheck/override")]
    public ActionResult<ApiCategoryOverrideCheckResult> RunCheckWithOverride(
        [FromBody] RunCheckOverrideRequest request
    )
    {
        try
        {
            var result = BeatmapService.RunDifficultyCheckWithOverride(
                request.Folder,
                request.DifficultyName,
                request.OverrideDifficulty
            );

            if (result == null)
                return NotFound(
                    new ApiError(
                        $"Difficulty '{request.DifficultyName}' not found in beatmap set.",
                        null
                    )
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                ApiErrorFactory.FromException(
                    ex,
                    "An error occurred while running beatmap check with override."
                )
            );
        }
    }

    [HttpDelete("checkRunHistory")]
    public ActionResult ClearCheckRunHistory([FromQuery] string folder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder))
                return BadRequest(new ApiError("Folder is required.", null));

            var beatmapSet = BeatmapService.ParseBeatmapSet(folder).BeatmapSet;
            CheckRunHistoryService.ClearHistory(beatmapSet);
            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clear check run history for {Folder}", folder);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(
                    ex,
                    "An error occurred while clearing check run history."
                )
            );
        }
    }
}
