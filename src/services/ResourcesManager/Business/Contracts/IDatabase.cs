using ResourcesManager.Business.DataModel;
using System.Linq.Expressions;

namespace ResourcesManager.Business.Contracts;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<int>> InsertAsync(Resource resource);
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
}

public interface IDatabaseQuery
{
    ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(Expression<Func<Resource, bool>> filter, int? limit = null);
}
