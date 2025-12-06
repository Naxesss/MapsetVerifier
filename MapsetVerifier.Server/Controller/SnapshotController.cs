using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("snapshot")]
public class SnapshotController : ControllerBase
{
    [HttpPost]
    public ActionResult<ApiSnapshotResult> GetSnapshots([FromBody] RunChecksRequest request)
    {
        try
        {
            var result = SnapshotService.GetSnapshots(request.Folder);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get snapshots for {Folder}", request.Folder);
            return StatusCode(500, new ApiError("An error occurred while getting snapshots.", ex.Message, ex.StackTrace));
        }
    }
}

