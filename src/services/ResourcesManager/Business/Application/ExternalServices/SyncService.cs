namespace ResourcesManager.Business.Application.ExternalServices.SyncService;

/// <summary>
/// Syncro service
/// </summary>
public interface ISyncService
{
    ValueTask<OkOrError<long>> GetSyncTimeAsync();
}
