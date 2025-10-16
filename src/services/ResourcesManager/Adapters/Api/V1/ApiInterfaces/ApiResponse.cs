using ResourcesManager.Business.Application;

namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;


public class MetadataPart()
{
    public DateTimeOffset ServiceTimeUTC { get; } = DateTimeOffset.UtcNow;
    public string Source { get; } = ResourceRules.SourceName;
    public string Version { get; } = ResourceRules.SourceVersion;   
}

public record DataPart(object Payload);

public record ErrorPart(string Message, int? Code = null);

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

    public ApiResponse() {}

    public static ApiResponse Empty(string? originRequestId = null) => new();

    public static ApiResponse DataResponse(object data, string? originRequestId = null) => new()
    {
        Data = new(data)
    };

    public static ApiResponse ErrorResponse(
        string errorMsg,
        int code = (int)ErrorCodes.GenericError) => new()
    {
        Error = new(errorMsg, code)
    };
}




