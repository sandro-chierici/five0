
using System.Text.Json;

namespace JsonIngestion.DataModels;

public record RequestDetail(string? Host, string? HostOrigin, long? FileLength, string UserId, DateTimeOffset TimestampUTC);

public record InputDataDto(JsonDocument Document, RequestDetail Request);