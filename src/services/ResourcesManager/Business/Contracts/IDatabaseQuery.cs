using ResourcesManager.Business.DataModel;
using System.Linq.Expressions;

namespace ResourcesManager.Business.Contracts;

public interface IDatabaseQuery
{
    ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter, int limit = ResourceRules.ResourcesQueryLimit);

    ValueTask<QueryResponse<List<Resource>>> GetResourcesAsync(string sql);
}
