using Microsoft.EntityFrameworkCore;
using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceCommand(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    IDbContextFactory<TenantContext> tenantContextFactory) : IDatabaseCommand
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
            return new() { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    public async ValueTask<QueryResponse<int>> InsertAsync(Resource resource)
    {
        return await Task.Run(() => new QueryResponse<int> { Results = 0 });
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

            // getting exact time fof transaction
            var localSystemNow = DateTimeOffset.UtcNow;

            // resource creation
            var resource = new Resource
            {
                Name = request.Name,
                TenantId = request.TenantId!.Value,
                Description = request.Description,
                ResourceTypeId = request.ResourceTypeId
            };
            await rctx.Resources.AddAsync(resource);

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
                    UtcCreatedDate = localSystemNow
                });
            }

            // generate event status for the new resource
            var initialStatus = new ResourceStatusHistory
            {
                ResourceId = resource.Id,
                ResourceStatusId = 1,
                UtcTime = localSystemNow,
                TenantId = request.TenantId.Value,
                Notes = "Initial status upon creation"
            };
            await rctx.ResourceStatusHistories.AddAsync(initialStatus);

            await rctx.SaveChangesAsync();

            return new QueryResponse<long> { Results = resource.Id };
        }
        catch (Exception ex)
        {
            return new QueryResponse<long> { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }
}
