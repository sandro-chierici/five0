using System.Text.Json.Serialization;
using ResourcesManager.Business.Application;

namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;


public record struct MetadataPart()
{
    [JsonPropertyName("utc-millis")]
    public long UtcMillis { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    [JsonPropertyName("source")]
    public string Source { get; init; } = ResourceRules.SourceName;
    [JsonPropertyName("api-version")]
    public string Version { get; init; } = ResourceRules.SourceVersion;    
}

public record DataPart(object Payload);

public record ErrorPart(string Message, string? Description, int? Code = null);

public class ApiResponse
{
    /// <summary>
    /// Metadati risposta
    /// </summary>
    public MetadataPart Metadata { get; } = new();
    /// <summary>
    /// Payload
    /// </summary>
    public DataPart? Data { get; init; } 
    /// <summary>
    /// Error
    /// </summary>
    public ErrorPart? Error { get; init; }

    public static ApiResponse Ok() => new();

    public static ApiResponse DataResponse(object data) => new()
    {
        Data = new(data)
    };

    public static ApiResponse ErrorResponse(
        string error,
        string description = "",
        int code = (int)ErrorCodes.GenericError) => new()
    {
        Error = new(error, description, code)
    };
}




