namespace ResourcesManager.Business.Application.ExternalServices;

public interface ITimeService
{
    ValueTask<DateTimeOffset> GetCurrentTimeAsync();
}
