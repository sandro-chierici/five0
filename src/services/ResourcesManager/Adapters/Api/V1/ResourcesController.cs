﻿using Microsoft.AspNetCore.Mvc;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataViews;
using System.Data.Common;

namespace ResourcesManager.Adapters.Api.V1;

[Route("api/v1/tenant/{tenantId:long}/resources")]
[ApiController]
public class ResourcesController(IDatabaseQuery dbQuery, IDatabaseCommand dbCommand) : ControllerBase
{
    /// <summary>
    /// Single resource
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:long}")]
    public async ValueTask<ActionResult<ApiResponse>> FindOne(long tenantId, long id)
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
    public async ValueTask<ActionResult<ApiResponse>> Search([FromBody] ApiQuery query, long tenantId)
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

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(ApiResponse.ErrorResponse("Resource name is required"));

            if (request.TenantId != null && request.TenantId != tenantId)
                return BadRequest(ApiResponse.ErrorResponse("TenantId into url and into request does not match"));
            request.TenantId = tenantId;

            // Create the resource (assuming IDatabaseCommand interface exists)
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
    public void DeleteOne(long id)
    {
    }
}
