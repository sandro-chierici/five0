using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Metadata;

namespace ResourcesManager.Business.Application;

public class QueryResponse<T>
{
    public QueryMetadata Metadata { get; init; } = new();
    public T? Results { get; set; }
    public Error? Error { get; init; }
    
    [JsonIgnore]
    public bool IsError { get => Error != null; }

    public class QueryMetadata
    {
        public long QueryExecutionMillis { get; set; }
        public long RowsCount { get; set; }
    }
}
