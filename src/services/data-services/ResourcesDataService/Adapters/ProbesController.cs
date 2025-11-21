using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Business.Application;

namespace ResourcesManager.Adapters.Api;

[Route("api/health")]
[ApiController]
public class ProbesController(IDatabaseQuery DBQuery, ILogger<ProbesController> logger) : ControllerBase
{
    [Route("live")]
    [HttpGet]
    public async ValueTask<IActionResult> Liveness()
    {
        // Here you can add any custom liveness checks if needed
        return await ValueTask.FromResult(Ok());
    }
    /// <summary>
    /// Readiness probes
    /// </summary>
    /// <returns></returns>
    [Route("ready")]
    [HttpGet]
    public async ValueTask<IActionResult> Readiness()
    {
        // Here you can add any custom readiness checks if needed
        return await ValueTask.FromResult(Ok());
    }


    [Route("startup")]
    [HttpGet]
    public async ValueTask<IActionResult> Startup()
    {
        try
        {
            var resp = await DBQuery.GetResourcesAsync(res => true, limit: 1);
            if (resp.IsError)
            {
                logger.LogError("Startup db init failed: {ErrorMessage}", resp.QueryError?.errorMessage);
                return StatusCode(500, "Database query failed during startup init.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Startup check failed: {ErrorMessage}", ex.Message);
            return StatusCode(500, $"Exception during startup check: {ex.Message}");
        }

        // Here you can add any custom startup checks if needed
        return await ValueTask.FromResult(Ok());
    }
}
