using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;
using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Mvc;

namespace MapsetVerifier.Server.Controller;

[ApiController]
[Route("documentation")]
public class DocumentationController : ControllerBase
{
    [HttpGet("general")]
    public IEnumerable<ApiDocumentationCheck> GetGeneral()
    {
        return DocumentationService.GetGeneralDocumentation();
    }

    [HttpGet("beatmap")]
    public IEnumerable<ApiDocumentationCheck> GetBeatmapDocumentation([FromQuery] Beatmap.Mode mode)
    {
        return DocumentationService.GetBeatmapDocumentation(mode);
    }

    [HttpGet("{id:int}/details")]
    public ActionResult<ApiDocumentationCheckDetails> GetDetails(int id)
    {
        var documentDetails = DocumentationService.GetDocumentationDetails(id);

        if (documentDetails == null)
        {
            return NotFound("Check doesn't have any documentation details");
        }
        
        return Ok(documentDetails);
    }
}