using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Business.Application;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<int>> InsertAsync(Resource resource);
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
}
