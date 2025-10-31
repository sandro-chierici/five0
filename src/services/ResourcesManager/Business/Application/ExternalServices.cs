namespace ResourcesManager.Business.Application.ExternalServices;

/// <summary>
/// Syncro service
/// </summary>
public interface ISyncService
{
    ValueTask<OkOrError<long>> GetSyncTimeAsync();
}

/// <summary>
/// Invio eventi a Kafka
/// </summary>
public interface IEventService
{
    ValueTask SendEventAsync(object ev);
}

