
using ResourcesManager.Business.Application;

namespace ResourcesManager.Infrastructure.Business;

public class Rules : IResourceRules
{
    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync()
    {
        // For now, just return the local time
        return await Task.FromResult(DateTimeOffset.UtcNow);
    }   

    public async ValueTask<Guid> CreatePKAsync(DateTimeOffset? seed = null)
    {
        seed ??= await GetCurrentTimeAsync();
        // For now, just return a new GUID
        return await Task.FromResult(Guid.CreateVersion7(seed.Value));
    }
}

