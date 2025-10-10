using ResourcesManager.Business.DataModel;

namespace ResourcesManager.Business.Application;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<int>> InsertAsync(Resource resource);
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
}
