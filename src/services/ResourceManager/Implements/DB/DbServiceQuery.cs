using Microsoft.EntityFrameworkCore;
using ResourceManager.Business.Contracts;
using ResourceManager.Business.DataModel;
using System.Linq.Expressions;

namespace ResourceManager.Implements.DB;

public class DbServiceQuery(IDbContextFactory<ResourceContext> contextFactory) : IDatabaseQuery
{
    public async ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(Expression<Func<Resource, bool>> filter)
    {
        try
        {
            using var ctx = await contextFactory.CreateDbContextAsync();
            var data = await ctx.Resources
                .Where(filter)
                .Take(1000) // security limit
                .ToListAsync();

            return new() { Value = data };
        }
        catch (Exception ex) 
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }
}
