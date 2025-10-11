using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/_commands")]
[ApiController]
public class UtilsController(IDatabaseCommand dbCommand) : ControllerBase
{
    [Route("createdb")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse>> EnsureDbCreated()
    {
        var res = await dbCommand.EnsureDBCreated();

        return ApiResponse.DataResponse(res);
    }
}
