using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("snapshot")]
public class SnapshotController : ControllerBase
{
    /// <summary> Captures the mapset as it is now, then returns all its snapshots, newest first. </summary>
    [HttpPost("history")]
    public IActionResult GetHistory([FromBody] RunChecksRequest request) =>
        Run("getting snapshots", () => SnapshotService.GetHistory(request.Folder));

    /// <summary> What changed between two snapshots of a mapset. </summary>
    [HttpPost("compare")]
    public IActionResult Compare([FromBody] SnapshotCompareRequest request) =>
        Run(
            "comparing snapshots",
            () => SnapshotService.Compare(request.SetKey, request.BaseId, request.TargetId)
        );

    /// <summary> A few objects of one difficulty around a change, before and after. </summary>
    [HttpPost("window")]
    public IActionResult GetWindow([FromBody] SnapshotWindowRequest request) =>
        Run(
            "reading a part of the song",
            () =>
                SnapshotService.GetWindow(
                    request.SetKey,
                    request.BaseId,
                    request.TargetId,
                    request.DifficultyKey,
                    request.Anchor,
                    request.AnchorEnd,
                    request.Offset
                )
        );

    /// <summary> A manual snapshot, optionally pinned as a milestone. </summary>
    [HttpPost("capture")]
    public IActionResult Capture([FromBody] SnapshotCaptureRequest request) =>
        Run("capturing a snapshot", () => SnapshotService.Capture(request.Folder, request.Pin));

    /// <summary> Sets or clears the milestone name of a snapshot. </summary>
    [HttpPost("pin")]
    public IActionResult SetPin([FromBody] SnapshotPinRequest request) =>
        Run(
            "pinning a snapshot",
            () => SnapshotService.SetPin(request.SetKey, request.Id, request.Pin)
        );

    private IActionResult Run<T>(string what, Func<T> action)
    {
        try
        {
            return Ok(action());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed {What}", what);
            return StatusCode(
                500,
                ApiErrorFactory.FromException(ex, "An error occurred while " + what + ".")
            );
        }
    }
}

public class SnapshotCompareRequest
{
    public string SetKey { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
}

public class SnapshotCaptureRequest
{
    public string Folder { get; set; } = string.Empty;
    public string? Pin { get; set; }
}

public class SnapshotPinRequest
{
    public string SetKey { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string? Pin { get; set; }
}

public class SnapshotWindowRequest
{
    public string SetKey { get; set; } = string.Empty;
    public string BaseId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string DifficultyKey { get; set; } = string.Empty;
    public double Anchor { get; set; }
    public double AnchorEnd { get; set; }

    /// <summary> How many objects the window has been scrolled from the change. </summary>
    public int Offset { get; set; }
}
