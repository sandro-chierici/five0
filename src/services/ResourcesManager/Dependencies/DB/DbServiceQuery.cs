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
            var tm = System.Diagnostics.Stopwatch.StartNew();

            var data = await ctx.Resources
                .Where(filter)
                .Take(limit > 0 ? limit : ResourceRules.ResourcesQueryLimit)
                .AsNoTracking()
                .ToListAsync();

            tm.Stop();

            return new()
            {
                Result = data,
                Metadata = {
                    ResultCount = data.Count,
                    ExecMillis = tm.ElapsedMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    public async ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(string sql)
    {
        try
        {
            using var ctx = await contextFactory.CreateDbContextAsync();

            var tm = System.Diagnostics.Stopwatch.StartNew();

            var data = await ctx.Database.SqlQueryRaw<Resource>(sql)
                .ToListAsync();

            tm.Stop();

            return new()
            {
                Result = data,
                Metadata =
                {
                    ResultCount = data.Count,
                    ExecMillis = tm.ElapsedMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }
}
