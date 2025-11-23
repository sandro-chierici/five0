using Microsoft.EntityFrameworkCore;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceCommand(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    ILogger<DbServiceCommand> logger) : IDatabaseCommand
{
    /// <summary>
    /// Create Database
    /// </summary>
    /// <returns></returns>
    public async ValueTask<QueryResponse<string?>> EnsureDBCreated()
    {
        try
        {
            using var rctx = await resourceContextFactory.CreateDbContextAsync();
            await rctx.Database.EnsureCreatedAsync();

            return new() { Results = "OK Cowboy" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring database is created");
            return new() { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    /// <summary>
    /// Create a new resource in the database.
    /// </summary>
    /// <param name="request">The request containing resource creation details.</param>
    /// <returns>A QueryResponse containing the ID of the created resource.</returns>
    public async ValueTask<QueryResponse<long>> CreateResourceGroupAsync(CreateResourceGroupRequest request)
    {
        try {
        // {
        //     using var rctx = await resourceContextFactory.CreateDbContextAsync();

        //     // getting exact time for transaction
        //     var localSystemNow = DateTimeOffset.UtcNow;
        //     // resource group creation
        //     var group = new ResourceGroup
        //     {
        //         ResourceGroupCode = request.Code,
        //         Description = request.Description,
        //         TenantId = request.TenantId!.Value,
        //         UtcCreated = localSystemNow
        //     };
        //     await rctx.ResourceGroups.AddAsync(group);

        //     await rctx.SaveChangesAsync();

            // var resp = new QueryResponse<long> { Results = group.Id };
            var resp = new QueryResponse<long> { Results = 1L };
            resp.Metadata.RowsInserted = 1;

            return await Task.FromResult(resp);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating resource group");
            return new QueryResponse<long> { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    /// <summary>
    /// Create a new resource in the database.
    /// </summary>
    /// <param name="request">The request containing resource creation details.</param>
    /// <returns>A QueryResponse containing the ID of the created resource.</returns>
    public async ValueTask<QueryResponse<string>> CreateResourceAsync(CreateResourceRequest request)
    {
        try
        {
            using var rctx = await resourceContextFactory.CreateDbContextAsync();

            // getting exact time for transaction
            var localSystemNow = DateTimeOffset.UtcNow;

            // resource creation
            var resource = new Resource
            {
                ResourceCode = request.ResourceCode!,
                TenantCode = request.TenantCode!,
                Description = request.Description,
                Name = request.Name,
                UtcCreated = localSystemNow,
                Metadata = (request.Metadata != null) ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) : null
            };
            await rctx.Resources.AddAsync(resource);

            await rctx.SaveChangesAsync();            

            // if resource group is provided, check if it exists and belongs to the tenant
            if (request.ResourceGroupCode != null)
            {
                var wellFormattedCode = request.ResourceGroupCode.Trim().ToLower();
                var group = await rctx.ResourceGroups
                    .FirstOrDefaultAsync(g => 
                                        g.TenantCode == request.TenantCode 
                                        && g.ResourceGroupCode.ToLower() == wellFormattedCode
                                        );
                if (group == null)
                {
                    return new QueryResponse<string>
                    {
                        QueryError = new Error($"Resource group {request.ResourceGroupCode} not found or does not belong to the tenant.", ErrorCodes.ValuesNotFound)
                    };
                }

                // add resource to the group
                await rctx.ResourceToGroups.AddAsync(new ResourceToGroup
                {
                    ResourceId = resource.ResourceId,
                    TenantCode = request.TenantCode!,
                    ResourceGroupId = group.ResourceGroupId,
                    UtcCreated = localSystemNow
                });
            }

            await rctx.SaveChangesAsync();

            return new QueryResponse<string> { Results = resource.ResourceCode };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating resource");
            return new QueryResponse<string> { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    /// <summary>
    ///  delete`s a resource from the database.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async ValueTask<QueryResponse<long>> DeleteResourceAsync(long tenantId, long id)
    {
        try
        {
            // using var rctx = await resourceContextFactory.CreateDbContextAsync();

            // // getting exact time for transaction
            // var localSystemNow = DateTimeOffset.UtcNow;

            // var res = await rctx.Resources.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
            // if (res == null)
            // {
            //     return new QueryResponse<long>
            //     {
            //         QueryError = new Error($"Resource not found tenantId {tenantId} id {id}.", ErrorCodes.ValuesNotFound)
            //     };
            // }

            // // remove resource from groups
            // var resourceGroups = rctx.ResourceResourceGroups.Where(rrg => rrg.ResourceId == res.Id);
            // rctx.ResourceResourceGroups.RemoveRange(resourceGroups);

            // // add deletions to resource status history
            // await rctx.ResourceEventStores.AddAsync(new ResourceEventStore
            // {
            //     ResourceId = res.Id,
            //     ResourceStatusId = 3, // assuming 3 is the 'deleted' status
            //     TenantId = res.TenantId,
            //     // print resource delete values as JSON
            //     Notes = System.Text.Json.JsonSerializer.Serialize(res),
            //     UtcCreated = localSystemNow
            // });

            // // remove the resource
            // rctx.Resources.Remove(res);

            // await rctx.SaveChangesAsync();

            var resp = new QueryResponse<long> { Results = 0 };
            resp.Metadata.RowsDeleted = 1;

            return await Task.FromResult(resp);    
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error deleting resource tenantId {tenantId} id {id}");
            return new QueryResponse<long> { QueryError = new Error($"Error deleting resource tenantId {tenantId} id {id}", ErrorCodes.GenericError) };
        }
    }
}
