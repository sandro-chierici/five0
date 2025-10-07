using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Contracts;
using ResourcesManager.Business.DataModel;
using System.Linq.Expressions;

namespace ResourcesManager.Dependencies.DB;

public class DbServiceQuery(IDbContextFactory<ResourceContext> contextFactory) : IDatabaseQuery
{
    public async ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        int limit = ResourceRules.ResourcesQueryLimit)
    {
        try
        {
            using var ctx = await contextFactory.CreateDbContextAsync();
            var data = await ctx.Resources
                .Where(filter)
                .Take(limit > 0 ? limit : ResourceRules.ResourcesQueryLimit) // security limit
                .ToListAsync();

            return new() { Value = data };
        }
        catch (Exception ex) 
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }
}
