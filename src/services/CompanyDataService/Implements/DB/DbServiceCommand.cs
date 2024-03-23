using Microsoft.EntityFrameworkCore;
using CompanyDataService.Business.Contracts;
using CompanyDataService.Business.DataModel;

namespace CompanyDataService.Implements.DB;

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
        return new QueryResponse<int> { Value = 0 };
    }
}
