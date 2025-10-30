using ResourcesManager.Business.Application.ExternalServices;

namespace Services.ResourcesManager.Infrastructure.Services;

/// <summary>
/// Registering any http client
/// </summary>
public static class ServicesClientsRegistration
{
    public const string TIME_SERVICE = "TimeServiceClient";
    public const string EVENT_SERVICE = "EventServiceClient";

    public static void AddTimeServiceClient(this IServiceCollection services, ConfigurationManager config)
    {
        var url = config["Five0:TimeServiceUrl"] ?? "http://localhost:6080";
        services.AddHttpClient(TIME_SERVICE, client =>
        {
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        Console.WriteLine($"Configured TimeService client at url {url}");
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


public class TimeServiceClient(IHttpClientFactory Factory) : ITimeService
{
    /// <summary>
    /// Definizione risposta TimeService
    /// </summary>
    /// <param name="utcUnixTime"></param>
    private record TimeServiceResponse(long? utcUnixTime);

    /// <summary>
    /// Restituisce sempre un valore 
    /// In caso di irraggiungibilita' del servizio di sincronizzazione da il tempo locale
    /// </summary>
    /// <returns></returns>
    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync()
    {
        try
        {
            var cl = Factory.CreateClient(ServicesClientsRegistration.TIME_SERVICE);
            var resp = await cl.GetAsync("/api/v1/now");
            resp.EnsureSuccessStatusCode();

            var content = await resp.Content.ReadFromJsonAsync<TimeServiceResponse>();
            if (content?.utcUnixTime == null)
                throw new Exception("TimeService responded with empty content");

            return DateTimeOffset.FromUnixTimeMilliseconds(content.utcUnixTime.Value);
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error retrieving Time from TimeService {e.Message}");
            return DateTimeOffset.UtcNow;
        }
    }
}

