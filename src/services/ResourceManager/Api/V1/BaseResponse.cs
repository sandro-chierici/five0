namespace ResourceManager.Api.V1;


public class BaseResponse
{
    public record MetadataResponse(string Source, string Ver)
    {
        public long UtcMillis { get; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public record DataResponse(object Value);

    public record ErrorResponse(string Message, string? Description, int? code = null);

    public MetadataResponse Metadata { get; }
    public DataResponse? Data { get; init; }

    public ErrorResponse? Error { get; init; }

    public BaseResponse(
        string source = "five0 Resource Manager", 
        string ver = "1.0")
    {
        Metadata = new MetadataResponse(source, ver);
    }

    public BaseResponse(
    string error,
    string? errorDescription = null,
    string source = "five0 Resource Manager",
    string ver = "1.0")
    {
        Metadata = new MetadataResponse(source, ver);
        Error = new ErrorResponse(error, errorDescription);
    }

    public BaseResponse(
    object data,
    string source = "five0 Resource Manager",
    string ver = "1.0")
    {
        Metadata = new MetadataResponse(source, ver);
        Data = new DataResponse(data);
    }
}

public class OkResponse() : BaseResponse();
public class DataResponse(object data) : BaseResponse(data: data);
public class ErrorResponse(string error, string? description = null) : BaseResponse(error: error, errorDescription: description);



