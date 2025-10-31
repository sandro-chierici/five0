using ResourcesManager.Business.Application.ExternalServices;

namespace Services.ResourcesManager.Infrastructure.Services;

/// <summary>
/// Registering any http client
/// </summary>
public static class ServicesClientsRegistration
{
    public const string SYNC_SERVICE = "SyncServiceClient";
    public const string EVENT_SERVICE = "EventServiceClient";

    public static void AddTimeServiceClient(this IServiceCollection services, ConfigurationManager config)
    {
        var url = config["Five0:SyncServiceUrl"] ?? "http://localhost:6080";
        services.AddHttpClient(SYNC_SERVICE, client =>
        {
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        Console.WriteLine($"Configured SyncService client at url {url}");
    }

    public static void AddEventServiceClient(this IServiceCollection services, ConfigurationManager config)
    {
        var url = config["Five0:EventServiceUrl"] ?? "http://localhost:6090";
        services.AddHttpClient(EVENT_SERVICE, client =>
        {
            client.BaseAddress = new Uri("http://localhost:6081");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        Console.WriteLine($"Configured EventService client at url {url}");
    }
}


public class SyncServiceClient(IHttpClientFactory Factory) : ISyncService
{
    /// <summary>
    /// Definizione risposta TimeService
    /// </summary>
    /// <param name="utcUnixTime"></param>
    private record SyncServiceResponse(long? utcUnixTime);

    /// <summary>
    /// Restituisce utc unix nanosecondi since 1/1/70
    /// </summary>
    /// <returns></returns>
    public async ValueTask<OkOrError<long>> GetSyncTimeAsync()
    {
        try
        {
            var cl = Factory.CreateClient(ServicesClientsRegistration.SYNC_SERVICE);
            var resp = await cl.GetAsync("/api/v1/now");
            resp.EnsureSuccessStatusCode();

            var content = await resp.Content.ReadFromJsonAsync<SyncServiceResponse>();
            if (content?.utcUnixTime == null)
                throw new Exception("TimeService responded with empty content");

            return new OkOrError<long>(content.utcUnixTime.Value);
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error retrieving Time from TimeService {e.Message}");
            return new OkOrError<long>($"Error retrieving Time from TimeService {e.Message}");
        }
    }
}

