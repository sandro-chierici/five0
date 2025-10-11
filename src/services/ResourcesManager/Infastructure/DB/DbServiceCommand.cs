using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Application;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Infrastructure.DB;

public class DbServiceCommand(IDbContextFactory<ResourceContext> contextFactory) : IDatabaseCommand
{
    /// <summary>
    /// Create Database
    /// </summary>
    /// <returns></returns>
    public async ValueTask<QueryResponse<string?>> EnsureDBCreated()
    {
        try
        {
            using var ctx = await contextFactory.CreateDbContextAsync();
            await ctx.Database.EnsureCreatedAsync();

            return new() { Results = "OK" };
        }
        catch (Exception ex)
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    public async ValueTask<QueryResponse<int>> InsertAsync(Resource resource)
    {
        return await Task.Run(() => new QueryResponse<int> { Results = 0 });
    }
}
