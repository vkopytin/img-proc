using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;

[Route("dam/[action]")]
[ApiController()]
public class DigitalAssetsManagerController : ControllerBase
{
  [HttpGet]
  [ActionName("{id}")]
  public async Task<IActionResult> ListArticleBlocks([FromRoute] string id)
  {
    return Ok(new { message = "not-implemented" });
  }
}
