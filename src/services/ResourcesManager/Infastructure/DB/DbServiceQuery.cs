using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataModel.Tenants;
using ResourcesManager.Business.DataViews;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceQuery(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    IDbContextFactory<TenantContext> tenantContextFactory) : IDatabaseQuery
{
    public async ValueTask<QueryResponse<List<ResourceView>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        int limit = ResourceRules.ResourcesQueryLimit)
    {
        try
        {
            using var resourceCtx = await resourceContextFactory.CreateDbContextAsync();
            using var tenantCtx = await tenantContextFactory.CreateDbContextAsync();
            var tm = System.Diagnostics.Stopwatch.StartNew();

            // Get resources with resource types in one query
            var resources = await (from resource in resourceCtx.Resources
                                   .Where(filter)
                                   .Take(limit)
                                   .AsNoTracking()
                                   join typ in resourceCtx.ResourceTypes
                                   on new { resource.TenantId, Id = resource.ResourceTypeId ?? 0 }
                                   equals new { typ.TenantId, typ.Id } into rt
                                   from resourceType in rt.DefaultIfEmpty()
                                   select new
                                   {
                                       Resource = resource,
                                       ResourceType = resourceType
                                   }).ToListAsync();

            // Get tenant IDs
            var tenantIds = resources
                .Where(r => r.Resource.TenantId != null)
                .Select(r => r.Resource.TenantId)
                .Distinct()
                .ToList();

            // Get all tenants in separate query
            var tenants = await tenantCtx.Tenants
                .Where(t => tenantIds.Contains(t.Id))
                .AsNoTracking()
                .ToDictionaryAsync(t => t.Id, t => t);

            // get all groups in a separate query
            var grps = (from r in resources
                        join rrg in resourceCtx.ResourceResourceGroups
                        on new { r.Resource.Id, r.Resource.TenantId } equals new { Id = rrg.ResourceId, rrg.TenantId }
                        join rg in resourceCtx.ResourceGroups
                        on new { rrg.ResourceGroupId, rrg.TenantId } equals new { ResourceGroupId = rg.Id, rg.TenantId }
                        select new
                        {
                            Resource = r,
                            Group = rg
                        }).ToList();

            // Combine results
            var data = resources.Select(r => new ResourceView
            {
                Resource = r.Resource,
                ResourceType = r.ResourceType,
                Tenant = tenants
                    .GetValueOrDefault(r.Resource.TenantId ?? 0),
                ResourceGroups = grps
                    .Where(g => g.Resource.Resource.Id == r.Resource.Id)
                    .Select(r => r.Group)
                    .ToList()
            }).ToList();

            tm.Stop();

            return new()
            {
                Results = data,
                Metadata =
                {
                    RowsRead = data.Count(),
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
