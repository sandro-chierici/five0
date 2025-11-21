using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataViews;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/tenant/{tenant:required}/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery, IDatabaseCommand dbCommand) : ControllerBase
{
    /// <summary>
    /// Single resource
    /// </summary>
    /// <param name="tenant"></param>
    /// <param name="resource"></param>
    /// <returns></returns>
    [HttpGet("{resource:required}")]
    public async ValueTask<ActionResult<ApiResponse>> FindOne(string tenant, string resource)
    {
        var resp = await dbQuery.GetResourcesAsync(res => 
            res.ResourceCode == resource && res.TenantCode == tenant);

        if (resp.IsError)
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request Error"));

        return Ok(ApiResponse.DataResponse(resp));
    }

    /// <summary>
    /// Search by query
    /// </summary>
    /// <param name="value"></param>
    [HttpPost("_search")]
    public async ValueTask<ActionResult<ApiResponse>> Search([FromBody] ApiQuery query, string tenant)
    {
        // inner func to evalueate query
        async ValueTask<QueryResponse<List<ResourceView>>> evaluateQuery(ApiQuery q) =>
            q switch
            {
                // get a sequence of ids
                { Ids: not null } and { Ids.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource => q.Ids.Contains(resource.ResourceCode) && resource.TenantCode == tenant),

                // get by name
                { StartsWith: not null } and { StartsWith.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource =>
                    resource.ResourceCode != null &&
                    resource.TenantCode == tenant &&
                    resource.ResourceCode.ToLower().StartsWith(q.StartsWith.ToLower())),

                // // get by typeid
                // { ResourceTypeId: not null } and { ResourceTypeId.Length: > 0 } =>
                // await dbQuery.GetResourcesAsync(
                //     resource =>
                //     resource.TenantCode == tenant &&
                //     resource.ResourceTypeId.HasValue &&
                //     q.ResourceTypeId.Contains(resource.ResourceTypeId.Value)),

                // // get by groupId
                // { ResourceGroupId: not null } and { ResourceGroupId.Length: > 0 } =>
                // await dbQuery.GetResourcesByGroupAsync(
                //     group => group.TenantId == tenantId &&
                //     q.ResourceGroupId.Contains(group.Id)),

                _ => new QueryResponse<List<ResourceView>>()
            };

        var resp = await evaluateQuery(query);
        if (resp.IsError)
        {
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request error"));
        }

        return Ok(ApiResponse.DataResponse(resp));
    }
    /// <summary>
    /// create a resource into tenant
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public async ValueTask<ActionResult<ApiResponse>> CreateOne(long tenantId, [FromBody] CreateResourceRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                return BadRequest(ApiResponse.ErrorResponse("Request body is required"));

            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(ApiResponse.ErrorResponse("Resource name is required"));

            if (request.TenantId != null && request.TenantId != tenantId)
                return BadRequest(ApiResponse.ErrorResponse("TenantId into url and into request does not match"));
            request.TenantId = tenantId;

            var createResult = await dbCommand.CreateResourceAsync(request);

            if (createResult.IsError)
                return BadRequest(ApiResponse.ErrorResponse(createResult.QueryError?.errorMessage ?? "Failed to create resource"));

            return Ok(ApiResponse.DataResponse(createResult));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Internal server error: {ex.Message}"));
        }
    }


    [HttpDelete("{id:long}")]
    public async ValueTask<ActionResult<ApiResponse>> DeleteOne(long tenantId, long id)
    {
        try
        {
            // Create the resource (assuming IDatabaseCommand interface exists)
            var res = await dbCommand.DeleteResourceAsync(tenantId, id);

            if (res.IsError)
                return BadRequest(ApiResponse.ErrorResponse(res.QueryError?.errorMessage ?? "Failed to delete resource"));

            return Ok(ApiResponse.Empty());
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Internal server error: {ex.Message}"));
        }
    }
}
