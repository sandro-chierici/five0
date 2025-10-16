using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataViews;
using System.Linq.Expressions;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceQuery(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    IDbContextFactory<TenantContext> tenantContextFactory) : IDatabaseQuery
{
    public async ValueTask<QueryResponse<List<ResourceView>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        Expression<Func<ResourceResourceGroup, bool>>? filterGroup = null,
        int limit = ResourceRules.ResourcesQueryLimit)
    {
        try
        {
            using var resourceContext = await resourceContextFactory.CreateDbContextAsync();
            using var tenantContext = await tenantContextFactory.CreateDbContextAsync();
            var tm = System.Diagnostics.Stopwatch.StartNew();

            // first create query 
            var query = resourceContext.Resources
                .Where(filter);

            // then add filter on resourcegroups
            if (filterGroup != null)
            {
                // search all resourceid with that groups
                var filteredIds = await resourceContext.ResourceResourceGroups
                .Where(filterGroup)
                .Select(rrg => rrg.ResourceId)
                .ToArrayAsync();

                query.Where(r => filteredIds.Contains(r.Id));
            }   

            // exec query
            var resourceList = await query
                .Take(limit > 0 ? limit : ResourceRules.ResourcesQueryLimit)
                .AsNoTracking()
                .ToListAsync();

            // read tenants 
            var tenantIds = resourceList.Select(r => r.TenantId)
                .Where(tid => tid.HasValue)
                .Distinct()
                .ToList();
            var tenantsDict = tenantIds.Any() ?
                await tenantContext.Tenants.Where(t => tenantIds.Contains(t.Id))
                .AsNoTracking()
                .ToDictionaryAsync(t => t.Id, tenant => tenant) :
                new Dictionary<long, Business.DataModel.Tenants.Tenant>();

            // read ResourceTypes
            var resourceTypeIds = resourceList.Select(r => r.ResourceTypeId)
                .Where(rtId => rtId.HasValue)
                .Distinct()
                .ToList();
            var resourceTypesDict = resourceTypeIds.Any() ?
                await resourceContext.ResourceTypes.Where(rt => resourceTypeIds.Contains(rt.Id))
                .AsNoTracking()
                .ToDictionaryAsync(rt => rt.Id, resourceType => resourceType) :
                new Dictionary<long, ResourceType>();


            // create resourceviews
            var data = resourceList.Select(r => new ResourceView
            {
                Resource = r,
                Tenant = r.TenantId.HasValue && tenantsDict.ContainsKey(r.TenantId.Value) ?
                    tenantsDict[r.TenantId.Value] : null,
                ResourceType = r.ResourceTypeId.HasValue && resourceTypesDict.ContainsKey(r.ResourceTypeId.Value) ?
                    resourceTypesDict[r.ResourceTypeId.Value] : null,
            }).ToList();

            tm.Stop();

            return new()
            {
                Results = data,
                Metadata =
                {
                    RowsRead = data.Count,
                    QueryExecutionMillis = tm.ElapsedMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            return new() { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    public async ValueTask<QueryResponse<List<object>>> GetResourcesSQLAsync(string sql)
    {
        try
        {
            using var ctx = await resourceContextFactory.CreateDbContextAsync();

            var tm = System.Diagnostics.Stopwatch.StartNew();

            var data = await ctx.Database.SqlQueryRaw<object>(sql)
                .ToListAsync();

            tm.Stop();

            return new()
            {
                Results = data,
                Metadata =
                {
                    RowsRead = data.Count,
                    QueryExecutionMillis = tm.ElapsedMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            return new() { QueryError = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }
}
