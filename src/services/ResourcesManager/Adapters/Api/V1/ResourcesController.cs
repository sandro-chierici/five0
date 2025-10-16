using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataViews;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery) : ControllerBase
{
    /// <summary>
    /// Single resource
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async ValueTask<ActionResult<ApiResponse>> GetById(long id)
    {
        var resp = await dbQuery.GetResourcesAsync(res => res.Id == id);

        if (resp.IsError)
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request Error"));

        return Ok(ApiResponse.DataResponse(resp));
    }

    /// <summary>
    /// For testing purpouse
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    // [HttpGet("tenant/{tenantId:long}/name/{name?}")]
    // public async ValueTask<ActionResult<ApiResponse>> GetByNameAndTenant(long tenantId, string? name)
    // {
    //     var resp = await dbQuery.GetResourcesAsync(res => res.TenantId == tenantId &&
    //         (name == null || res.Name != null && res.Name.ToLower().StartsWith(name.ToLower())));

    //     if (resp.IsError)
    //     {
    //         return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request error"));
    //     }

    //     return Ok(ApiResponse.DataResponse(resp));
    // }

    /// <summary>
    /// Search by query
    /// </summary>
    /// <param name="value"></param>
    [HttpPost("tenant/{tenantId:long}/search")]
    public async ValueTask<ActionResult<ApiResponse>> Post([FromBody] ApiQuery query, long tenantId)
    {
        // inner func to evalueate query
        async ValueTask<QueryResponse<List<ResourceView>>> evaluateQuery(ApiQuery q) =>
            q switch
            {
                // get a sequence of ids
                { Id: not null } and { Id.Length: > 0 } => await dbQuery.GetResourcesAsync(r => q.Id.Contains(r.Id) && r.TenantId == tenantId),
                // get by name
                { StartsWith: not null } and { StartsWith.Length: > 0 } => await dbQuery.GetResourcesAsync(r =>
                    r.Name != null &&
                    r.TenantId == tenantId &&
                    r.Name.ToLower().StartsWith(q.StartsWith.ToLower())),

                _ => new QueryResponse<List<ResourceView>>()
            };

        var resp = await evaluateQuery(query);
        if (resp.IsError)
        {
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request error"));
        }

        return Ok(ApiResponse.DataResponse(resp));
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
