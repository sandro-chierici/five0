using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataViews;
using System.Collections.Immutable;
using System.Linq.Expressions;


namespace ResourcesManager.Infrastructure.DB;

public class DbServiceQuery(
    IDbContextFactory<ResourceContext> resourceContextFactory,
    IDbContextFactory<TenantContext> tenantContextFactory) : IDatabaseQuery
{
    /// <summary>
    /// Resource List
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public async ValueTask<QueryResponse<List<ResourceView>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        int limit = ResourceRules.ResourcesQueryLimit)
    {
        try
        {
            using var resourceCtx = await resourceContextFactory.CreateDbContextAsync();
            using var tenantCtx = await tenantContextFactory.CreateDbContextAsync();

            // disable tracking we are read only
            resourceCtx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            tenantCtx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var tm = System.Diagnostics.Stopwatch.StartNew();

            var resources = await (from resource in resourceCtx.Resources.Where(filter)
                                   join typ in resourceCtx.ResourceTypes
                                   on new { resource.TenantId, Id = resource.ResourceTypeId ?? 0 }
                                   equals new { typ.TenantId, typ.Id } into rt
                                   from resourceType in rt.DefaultIfEmpty()
                                   select new
                                   {
                                       Resource = resource,
                                       ResourceType = resourceType
                                   })
                                  .Take(limit)
                                  .ToListAsync();


            // Get tenant IDs
            var tenantIds = resources
                .Select(r => r.Resource.TenantId)
                .Distinct()
                .ToList();

            // Get all tenants in separate query
            var tenants = await tenantCtx.Tenants
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t);

            // get all groups in a separate query
            var grps = (from rv in resources
                        join rrg in resourceCtx.ResourceResourceGroups
                        on new { rv.Resource.Id, rv.Resource.TenantId } equals new { Id = rrg.ResourceId, rrg.TenantId }
                        join rg in resourceCtx.ResourceGroups
                        on new { rrg.ResourceGroupId, rrg.TenantId } equals new { ResourceGroupId = rg.Id, rg.TenantId }
                        select new
                        {
                            rv.Resource,
                            rv.ResourceType,
                            Group = rg
                        }).ToList();

            // get statuses
            var sts = (from r in resources
                       join rsh in resourceCtx.ResourceStatusHistories
                       on new { r.Resource.Id, r.Resource.TenantId } equals new { Id = rsh.ResourceId, rsh.TenantId }
                       join rs in resourceCtx.ResourceStatuses
                       on new { rsh.TenantId, Id = rsh.ResourceStatusId } equals new { rs.TenantId, rs.Id }
                       orderby rsh.UtcCreated descending
                       select new
                       {
                           ResourceId = r.Resource.Id,
                           r.Resource.TenantId,
                           Status = rs
                       }).ToList();

            // Combine results
            var data = resources.Select(r => new ResourceView
            {
                Resource = r.Resource,
                ResourceType = r.ResourceType,
                Tenant = tenants
                    .GetValueOrDefault(r.Resource.TenantId),
                CurrentStatus = sts
                    .Where(s => s.ResourceId == r.Resource.Id && s.TenantId == r.Resource.TenantId)
                    .FirstOrDefault()?.Status,
                ResourceGroups = grps
                    .Where(g => g.Resource.Id == r.Resource.Id && g.Resource.TenantId == r.Resource.TenantId)
                    .Select(r => r.Group)
                    .ToList()
            })
            .ToList();

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


    /// <summary>
    /// List filtered by group
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public async ValueTask<QueryResponse<List<ResourceView>>> GetResourcesByGroupAsync(
         Expression<Func<ResourceGroup, bool>> filter,
         int limit = ResourceRules.ResourcesQueryLimit)
    {
        try
        {
            using var resourceCtx = await resourceContextFactory.CreateDbContextAsync();
            using var tenantCtx = await tenantContextFactory.CreateDbContextAsync();

            // disable tracking we are read only
            resourceCtx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            tenantCtx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            var tm = System.Diagnostics.Stopwatch.StartNew();

            // get all groups
            var resources = await (from g in resourceCtx.ResourceGroups.Where(filter)
                                   join rrg in resourceCtx.ResourceResourceGroups on g.Id equals rrg.ResourceGroupId
                                   join r in resourceCtx.Resources on rrg.ResourceId equals r.Id
                                   join t in resourceCtx.ResourceTypes on
                                   new { r.TenantId, Id = r.ResourceTypeId ?? 0 } equals new { t.TenantId, t.Id } into tg
                                   from resourceType in tg.DefaultIfEmpty()
                                   select new
                                   {
                                       Resource = r,
                                       ResourceType = resourceType,
                                       Group = g
                                   })
                                  .Take(limit)
                                  .ToListAsync();


            // Get tenant IDs
            var tenantIds = resources
                .Select(r => r.Resource.TenantId)
                .Distinct()
                .ToList();

            // Get all tenants in separate query
            var tenants = await tenantCtx.Tenants
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t);

            // get resources keys in memory
            var resourceIds = resources
                .Select(r => new { r.Resource.Id, r.Resource.TenantId })
                .Distinct()
                .ToList();

            // get statuses against Db
            var sts = (from r in resourceIds
                       join rsh in resourceCtx.ResourceStatusHistories
                       on new { r.Id, r.TenantId } equals new { Id = rsh.ResourceId, rsh.TenantId }
                       join rs in resourceCtx.ResourceStatuses
                       on new { rsh.TenantId, Id = rsh.ResourceStatusId } equals new { rs.TenantId, rs.Id }
                       orderby rsh.UtcCreated descending
                       select new
                       {
                           ResourceId = r.Id,
                           r.TenantId,
                           Status = rs
                       }).ToList();

            // Combine results
            var data = resourceIds
            .Select(r => new ResourceView
            {
                Resource = resources
                    .FirstOrDefault(res => res.Resource.Id == r.Id && res.Resource.TenantId == r.TenantId)?.Resource ?? new(),
                ResourceType = resources
                    .FirstOrDefault(res => res.Resource.Id == r.Id && res.Resource.TenantId == r.TenantId)?.ResourceType ?? new(),
                Tenant = tenants.GetValueOrDefault(r.TenantId),
                CurrentStatus = sts
                    .Where(s => s.ResourceId == r.Id && s.TenantId == r.TenantId)
                    .FirstOrDefault()?.Status,
                ResourceGroups = resources
                    .Where(res => res.Resource.Id == r.Id && res.Resource.TenantId == r.TenantId)
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
}

