using ResourcesManager.Adapters.Api.V1.ApiInterfaces;

namespace ResourcesManager.Business.Application;

public interface IDatabaseCommand
{
    ValueTask<QueryResponse<string?>> EnsureDBCreated();
    ValueTask<QueryResponse<string>> CreateResourceAsync(CreateResourceRequest request);
    ValueTask<QueryResponse<long>> DeleteResourceAsync(long tenantId, long id);
    ValueTask<QueryResponse<long>> CreateResourceGroupAsync(CreateResourceGroupRequest request);
}
