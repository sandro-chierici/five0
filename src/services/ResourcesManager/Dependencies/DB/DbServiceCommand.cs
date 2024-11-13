using Microsoft.EntityFrameworkCore;
using ResourcesManager.Business.Contracts;
using ResourcesManager.Business.DataModel;

namespace ResourcesManager.Dependencies.DB;

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

            return new() { Value = "OK" };
        }
        catch (Exception ex)
        {
            return new() { Error = new Error(ex.Message, ErrorCodes.GenericError) };
        }
    }

    public async ValueTask<QueryResponse<int>> InsertAsync(Resource resource)
    {
        return await Task.Run(() =>  new QueryResponse<int> { Value = 0 });
    }
}
