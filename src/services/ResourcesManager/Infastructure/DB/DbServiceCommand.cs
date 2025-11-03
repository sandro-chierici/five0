using Microsoft.EntityFrameworkCore;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceCommand(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    IDbContextFactory<TenantContext> tenantContextFactory,
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

            using var tctx = await tenantContextFactory.CreateDbContextAsync();
            await tctx.Database.EnsureCreatedAsync();

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
        try
        {
            using var rctx = await resourceContextFactory.CreateDbContextAsync();

            // getting exact time for transaction
            var localSystemNow = DateTimeOffset.UtcNow;
            var group = new ResourceGroup
            {
                Name = request.Name,
                Description = request.Description,
                TenantId = request.TenantId!.Value,
                UtcCreated = localSystemNow
            };
            await rctx.ResourceGroups.AddAsync(group);

            await rctx.SaveChangesAsync();

            var resp = new QueryResponse<long> { Results = group.Id };
            resp.Metadata.RowsInserted = 1;

            return resp;
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
    public async ValueTask<QueryResponse<long>> CreateResourceAsync(CreateResourceRequest request)
    {
        try
        {
            using var rctx = await resourceContextFactory.CreateDbContextAsync();

            // getting exact time for transaction
            var localSystemNow = DateTimeOffset.UtcNow;

            // resource creation
            var resource = new Resource
            {
                Code = request.Code,
                TenantId = request.TenantId!.Value,
                Description = request.Description,
                ResourceTypeId = request.ResourceTypeId,
                UtcCreated = localSystemNow
            };
            await rctx.Resources.AddAsync(resource);

            await rctx.SaveChangesAsync();            

            // if resource group is provided, check if it exists and belongs to the tenant
            if (request.ResourceGroupId.HasValue)
            {
                var group = await rctx.ResourceGroups
                    .FirstOrDefaultAsync(g => g.Id == request.ResourceGroupId.Value && g.TenantId == request.TenantId.Value);
                if (group == null)
                {
                    return new QueryResponse<long>
                    {
                        QueryError = new Error("Resource group not found or does not belong to the tenant.", ErrorCodes.ValuesNotFound)
                    };
                }

                // add resource to the group
                await rctx.ResourceResourceGroups.AddAsync(new ResourceResourceGroup
                {
                    ResourceGroupId = group.Id,
                    ResourceId = resource.Id,
                    TenantId = request.TenantId.Value,
                    UtcCreated = localSystemNow
                });
            }

            // generate event status for the new resource
            var initialStatus = new ResourceStatusHistory
            {
                ResourceId = resource.Id,
                ResourceStatusId = 1,
                TenantId = request.TenantId.Value,
                Notes = "Initial status upon creation",
                UtcCreated = localSystemNow
            };
            await rctx.ResourceStatusHistories.AddAsync(initialStatus);

            await rctx.SaveChangesAsync();

            return new QueryResponse<long> { Results = resource.Id };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating resource");
            return new QueryResponse<long> { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
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
            using var rctx = await resourceContextFactory.CreateDbContextAsync();

            // getting exact time for transaction
            var localSystemNow = DateTimeOffset.UtcNow;

            var res = await rctx.Resources.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
            if (res == null)
            {
                return new QueryResponse<long>
                {
                    QueryError = new Error($"Resource not found tenantId {tenantId} id {id}.", ErrorCodes.ValuesNotFound)
                };
            }

            // remove resource from groups
            var resourceGroups = rctx.ResourceResourceGroups.Where(rrg => rrg.ResourceId == res.Id);
            rctx.ResourceResourceGroups.RemoveRange(resourceGroups);

            // add deletions to resource status history
            await rctx.ResourceStatusHistories.AddAsync(new ResourceStatusHistory
            {
                ResourceId = res.Id,
                ResourceStatusId = 3, // assuming 3 is the 'deleted' status
                TenantId = res.TenantId,
                // print resource delete values as JSON
                Notes = System.Text.Json.JsonSerializer.Serialize(res),
                UtcCreated = localSystemNow
            });

            // remove the resource
            rctx.Resources.Remove(res);

            await rctx.SaveChangesAsync();

            var resp = new QueryResponse<long> { Results = res.Id };
            resp.Metadata.RowsDeleted = 1;

            return resp;    
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error deleting resource tenantId {tenantId} id {id}");
            return new QueryResponse<long> { QueryError = new Error($"Error deleting resource tenantId {tenantId} id {id}", ErrorCodes.GenericError) };
        }
    }
}
