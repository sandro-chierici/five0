using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataViews;
using System.Linq.Expressions;

namespace ResourcesManager.Business.Application;

public interface IDatabaseQuery
{
    ValueTask<QueryResponse<List<ResourceView>>> GetResourcesAsync(
        Expression<Func<Resource, bool>> filter,
        int limit = ResourceRules.ResourcesQueryLimit);

    ValueTask<QueryResponse<List<ResourceView>>> GetResourcesByGroupAsync(
        Expression<Func<ResourceGroup, bool>> filter,
        int limit = ResourceRules.ResourcesQueryLimit);        
     
}
