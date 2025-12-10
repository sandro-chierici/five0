
namespace ResourcesManager.Business.Application;

public interface IResourceRules
{
    /// <summary>
    /// Get current time from TimeService
    /// </summary>
    /// <returns></returns>
    ValueTask<DateTimeOffset> GetCurrentTimeAsync();
    /// <summary>
    /// Create a new PK 
    /// </summary>
    /// <returns></returns>
    ValueTask<Guid> CreatePKAsync(DateTimeOffset? seed = null);
}