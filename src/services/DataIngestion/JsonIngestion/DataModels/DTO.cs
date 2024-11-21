
using System.Text.Json;

namespace JsonIngestion.DataModels;

public record RequestDetail(string? Host, string? HostOrigin, long? fileLength, DateTimeOffset TimestampUTC);

public record InputDataDto(JsonDocument Document, RequestDetail? request);