namespace ResourcesManager.Business.Application.ExternalServices.EventService;

/// <summary>
/// Invio eventi a Kafka
/// </summary>
public interface IEventService
{
    ValueTask SendEventAsync(object ev);
}
