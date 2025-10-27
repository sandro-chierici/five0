namespace ResourcesManager.Business.Application.ExternalServices;

/// <summary>
/// Syncro service
/// </summary>
public interface ITimeService
{
    ValueTask<DateTimeOffset> GetCurrentTimeAsync();
}

/// <summary>
/// Invio eventi a Kafka
/// </summary>
public interface IEventService
{
    ValueTask SendEventAsync(object ev);
}

