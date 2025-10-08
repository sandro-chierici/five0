using Microsoft.AspNetCore.Http.Metadata;

namespace ResourcesManager.Business.Contracts;

public class QueryResponse<T>
{
    public QueryMetadata Metadata { get; init; } = new();
    public T? Result { get; set; }
    public Error? Error { get; init; }
    public bool IsError { get => Error != null; }

    public class QueryMetadata
    {
        public long ExecMillis { get; set; }
        public long ResultCount { get; set; }
    }
}
