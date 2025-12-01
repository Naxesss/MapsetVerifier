using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> GetHealth()
    {
        return Ok("up");
    }
}