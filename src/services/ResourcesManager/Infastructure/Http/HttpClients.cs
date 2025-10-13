using ResourcesManager.Business.Application.ExternalServices;

namespace Services.ResourcesManager.Infrastructure.Http;

/// <summary>
/// Registering any http client
/// </summary>
public static class HttpClientsRegistration
{
    public const string TimeServiceClient = "TimeServiceClient";

    public static void AddTimeServiceClient(this IServiceCollection services)
    {
        services.AddHttpClient(TimeServiceClient, client =>
        {
            client.BaseAddress = new Uri("http://localhost:6080");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
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
        var _httpClient = HttpClientFactory.CreateClient(HttpClientsRegistration.TimeServiceClient);

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
