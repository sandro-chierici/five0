using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataViews;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/tenant/{tenantId:long}/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery) : ControllerBase
{
    /// <summary>
    /// Single resource
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async ValueTask<ActionResult<ApiResponse>> GetById(long id, long tenantId)
    {
        var resp = await dbQuery.GetResourcesAsync(res => res.Id == id && res.TenantId == tenantId);

        if (resp.IsError)
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request Error"));

        return Ok(ApiResponse.DataResponse(resp));
    }

    /// <summary>
    /// Search by query
    /// </summary>
    /// <param name="value"></param>
    [HttpPost("_search")]
    public async ValueTask<ActionResult<ApiResponse>> Post([FromBody] ApiQuery query, long tenantId)
    {
        // inner func to evalueate query
        async ValueTask<QueryResponse<List<ResourceView>>> evaluateQuery(ApiQuery q) =>
            q switch
            {
                // get a sequence of ids
                { Id: not null } and { Id.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource => q.Id.Contains(resource.Id) && resource.TenantId == tenantId),

                // get by name
                { StartsWith: not null } and { StartsWith.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource =>
                    resource.Name != null &&
                    resource.TenantId == tenantId &&
                    resource.Name.ToLower().StartsWith(q.StartsWith.ToLower())),

                // get by typeid
                { ResourceTypeId: not null } and { ResourceTypeId.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource =>
                    resource.TenantId == tenantId &&
                    resource.ResourceTypeId.HasValue &&
                    q.ResourceTypeId.Contains(resource.ResourceTypeId.Value)),

                // get by groupId
                { ResourceGroupId: not null } and { ResourceGroupId.Length: > 0 } =>
                await dbQuery.GetResourcesByGroupAsync(
                    group => group.TenantId == tenantId &&
                    q.ResourceGroupId.Contains(group.Id)),

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
