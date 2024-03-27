using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Business.Contracts;

namespace ResourcesManager.Api.V1;

[Route("api/v1/[Controller]")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery, IDatabaseCommand dbCommand) : ControllerBase
{
    [Route("actions/createdb")]
    [HttpGet]
    public async Task<ActionResult<OkResponse>> EnsureDbCreated()
    {
        await dbCommand.EnsureDBCreated();

        return Ok(new OkResponse());
    }


    [HttpGet]
    public async Task<ActionResult<ApiBaseResponse>> GetAll()
    {
        var resp = await dbQuery.GetResourcesAsync(_ => true);

        if (resp.IsError)
        {
            return NotFound(new ErrorResponse(resp.Error?.errorMessage ?? "Not specified error"));
        }

        return Ok(new DataResponse(resp.Value?.ToList() ?? new()));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiBaseResponse>> GetById(long id)
    {
        var resp = await dbQuery.GetResourcesAsync(res => res.Id == id);

        if (resp.IsError)
        {
            return NotFound(new ErrorResponse(resp.Error?.errorMessage ?? "Not specified error"));
        }

        return Ok(new DataResponse(resp.Value?.ToList() ?? new()));
    }

    [HttpPost]
    public void Post([FromBody] string value)
    {
        
    }

    [HttpPut("{id:long}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    [HttpDelete("{id:long}")]
    public void Delete(long id)
    {
    }
}
