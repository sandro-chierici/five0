using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse>> GetById(long id)
    {
        var resp = await dbQuery.GetResourcesAsync(res => res.Id == id);

        if (resp.IsError)
        {
            return BadRequest(ApiResponse.ErrorResponse(resp.Error?.errorMessage ?? "Request Error"));
        }

        return Ok(ApiResponse.DataResponse(resp));
    }

    [HttpGet("tenant/{id:long}/{name?}")]
    public async Task<ActionResult<ApiResponse>> GetByNameAndTenant(long id, string? name)
    {
        var resp = await dbQuery.GetResourcesAsync(res => res.TenantId == id &&
            (name == null || res.Name != null && res.Name.ToLower().StartsWith(name.ToLower())));

        if (resp.IsError)
        {
            return BadRequest(ApiResponse.ErrorResponse(resp.Error?.errorMessage ?? "Request error"));
        }

        return Ok(ApiResponse.DataResponse(resp));
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
