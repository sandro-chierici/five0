using ResourcesManager.Business.Application.ExternalServices;

namespace Services.ResourcesManager.Infrastructure.Http;

/// <summary>
/// Registering any http client
/// </summary>
public static class HttpClientsRegistration
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


/// <summary>
/// Time Service implementation using HttpClient.
/// </summary>
public class TimeServiceClient(IHttpClientFactory HttpClientFactory, ILogger Logger) : ITimeService
{
    /// <summary>
    /// Data returned from TimeService
    /// </summary>
    private class TimeServiceResponse
    {
        public long UtcUnixTime { get; set; }
    }

    public async ValueTask<DateTimeOffset> GetCurrentTimeAsync()
    {
        var _httpClient = HttpClientFactory.CreateClient(HttpClientsRegistration.TIME_SERVICE);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<TimeServiceResponse>("api/v1/now");
            if (response is null)
            {
                return DateTimeOffset.UtcNow;
            }

            return new DateTimeOffset(response.UtcUnixTime, TimeSpan.Zero);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error calling TimeService {error}", ex.Message);
        }

        return DateTimeOffset.UtcNow;
    }
}
