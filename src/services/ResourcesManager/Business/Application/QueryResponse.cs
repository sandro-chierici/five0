using System.Text.Json.Serialization;

namespace ResourcesManager.Business.Application;

public enum ErrorCodes
{
    GenericError = 999, 
    ValuesNotFound = 100,
    QueryError = 1
}

public record Error(string? errorMessage, ErrorCodes? errorCode);

public class QueryResponse<T>
{
    public QueryMetadata Metadata { get; init; } = new();
    public T? Results { get; set; }
    public Error? QueryError { get; init; }

    [JsonIgnore]
    public bool IsError { get => QueryError != null; }

    public class QueryMetadata
    {
        public long QueryExecutionMillis { get; set; }
        public long RowsRead { get; set; }
        public long RowsInserted { get; set; }
        public long RowsUpdated { get; set; }
        public long RowsDeleted { get; set; }
    }
}
