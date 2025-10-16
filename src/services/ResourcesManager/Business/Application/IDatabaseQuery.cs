using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataViews;
using System.Linq.Expressions;

namespace ResourcesManager.Business.Application;

public interface IDatabaseQuery
{
    ValueTask<QueryResponse<List<ResourceView>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        Expression<Func<ResourceResourceGroup, bool>>? filterGroup = null,
        int limit = ResourceRules.ResourcesQueryLimit);

    ValueTask<QueryResponse<List<object>>> GetResourcesSQLAsync(string sql);
}
