using ResourcesManager.Business.DataModel;

namespace ResourcesManager.Business.Contracts;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<int>> InsertAsync(Resource resource);
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
}
