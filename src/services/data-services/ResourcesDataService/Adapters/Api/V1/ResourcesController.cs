using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataViews;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/tenant/{tenantId:required}/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery, IDatabaseCommand dbCommand) : ControllerBase
{
    /// <summary>
    /// Single resource
    /// </summary>
    /// <param name="tenant"></param>
    /// <param name="resourceCode"></param>
    /// <returns></returns>
    [HttpGet("{resourceCode:required}")]
    public async ValueTask<ActionResult<ApiResponse>> FindOne(string tenantId, string resourceCode)
    {
        // var resource = resourceCode.Normalized();
        var resp = await dbQuery.GetResourcesAsync(res => 
            res.ResourceCode == resourceCode 
            && res.TenantId.ToString() == tenantId
            );
        if (resp.IsError)
            return BadRequest(ApiResponse.ErrorResponse(resp.QueryError?.errorMessage ?? "Request Error"));

        return Ok(ApiResponse.DataResponse(resp));
    }

    /// <summary>
    /// Search by query
    /// </summary>
    /// <param name="value"></param>
    [HttpPost("_search")]
    public async ValueTask<ActionResult<ApiResponse>> Search([FromBody] ApiQuery query, string tenantId)
    {
        // inner func to evalueate query
        async ValueTask<QueryResponse<List<ResourceView>>> evaluateQuery(ApiQuery q) =>
            q switch
            {
                // get a sequence of ids
                { ResourceCode: not null } and { ResourceCode.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource => resource.TenantId.ToString() == tenantId && 
                    q.ResourceCode.Contains(resource.ResourceCode)
                    ),

                // get by name
                { NameStartsWith: not null } and { NameStartsWith.Length: > 0 } =>
                await dbQuery.GetResourcesAsync(
                    resource =>
                    resource.TenantId.ToString() == tenantId &&
                    resource.ResourceCode.ToLower().Trim().StartsWith(q.NameStartsWith.ToLower().Trim())
                    ),

                // // get by typeid
                // { ResourceTypeCode: not null } and { ResourceTypeCode.Length: > 0 } =>
                // await dbQuery.GetResourcesAsync(
                //     resource =>
                //     resource.TenantCode == tenant &&
                //     resource.ResourceType != null &&
                //     q.ResourceTypeCode.Contains(resource.ResourceType.ResourceTypeCode)),

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
    public async ValueTask<ActionResult<ApiResponse>> CreateOne(string tenantId, [FromBody] CreateResourceRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                return BadRequest(ApiResponse.ErrorResponse("Request body is required"));

            if (string.IsNullOrWhiteSpace(request.ResourceCode))
                return BadRequest(ApiResponse.ErrorResponse("Resource code is required"));

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


    // [HttpDelete("{id:long}")]
    // public async ValueTask<ActionResult<ApiResponse>> DeleteOne(long tenantId, long id)
    // {
    //     try
    //     {
    //         // Create the resource (assuming IDatabaseCommand interface exists)
    //         var res = await dbCommand.DeleteResourceAsync(tenantId, id);

    //         if (res.IsError)
    //             return BadRequest(ApiResponse.ErrorResponse(res.QueryError?.errorMessage ?? "Failed to delete resource"));

    //         return Ok(ApiResponse.Empty());
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, ApiResponse.ErrorResponse($"Internal server error: {ex.Message}"));
    //     }
    // }
}
