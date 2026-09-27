using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("ranking-criteria")]
public class RankingCriteriaController : ControllerBase
{
    [HttpGet]
    public ApiRcOverview GetOverview()
    {
        return RankingCriteriaService.GetOverview();
    }

    [HttpGet("pages/{key}")]
    public ActionResult<ApiRcPage> GetPage(string key)
    {
        var page = RankingCriteriaService.GetPage(key);

        if (page == null)
            return NotFound($"No ranking criteria page \"{key}\" in the snapshot");

        return Ok(page);
    }

    /// <summary> Statements by id, e.g. <c>?ids=osu/general/a&amp;ids=general/audio/b</c>. </summary>
    [HttpGet("statements")]
    public List<ApiRcStatement> GetStatements([FromQuery] string[] ids)
    {
        return RankingCriteriaService.GetStatements(ids);
    }
}
