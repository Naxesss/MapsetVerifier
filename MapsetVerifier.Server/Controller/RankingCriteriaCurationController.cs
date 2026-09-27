#if DEBUG
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

/// <summary> Edits the ranking criteria catalogue from the app. Debug builds only. </summary>
[ApiController]
[Route("ranking-criteria/curation")]
public class RankingCriteriaCurationController : ControllerBase
{
    [HttpPut]
    public ActionResult<ApiRcStatement> UpdateCuration([FromBody] ApiRcCurationRequest request)
    {
        var statement = RankingCriteriaService.UpdateCuration(
            request.Id,
            request.Automation,
            request.Notes
        );

        if (statement == null)
            return NotFound($"No ranking criteria statement \"{request.Id}\" in the snapshot");

        return Ok(statement);
    }
}
#endif
