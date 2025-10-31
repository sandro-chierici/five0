using Microsoft.AspNetCore.Mvc;

namespace ResourcesManager.Adapters.Api;

[Route("api/probes")]
[ApiController]
public class ProbesController() : ControllerBase
{
    /// <summary>
    /// Readiness probes
    /// </summary>
    /// <returns></returns>
    [Route("healtz")]
    [HttpGet]
    public async Task<ActionResult> Ready() => Ok();
}
