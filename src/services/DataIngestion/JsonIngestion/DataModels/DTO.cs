
using System.Text.Json;

namespace JsonIngestion.DataModels;

public record InputDataDto(JsonDocument Document, string HostSource, DateTimeOffset timestampUTC);