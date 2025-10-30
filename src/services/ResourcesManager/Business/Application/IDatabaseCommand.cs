using ResourcesManager.Adapters.Api.V1.ApiInterfaces;
using ResourcesManager.Business.DataModel.Resources;

namespace ResourcesManager.Business.Application;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
    ValueTask<QueryResponse<long>> CreateResourceAsync(CreateResourceRequest request);
}
