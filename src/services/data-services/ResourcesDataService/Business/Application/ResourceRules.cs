namespace ResourcesManager.Business.Application;

public static class ResourceRules
{
    public const int MaxResourceNameLength = 100;
    public const int MaxResourceDescriptionLength = 500;
    public const int MaxResourceExternalIdLength = 100;
    public const int MaxResourceTypeNameLength = 100;
    public const int MaxResourceTypeDescriptionLength = 500;
    public const int MaxTenantNameLength = 100;
    public const int MaxTenantDescriptionLength = 500;
    public const int MaxResourceGroupNameLength = 100;
    public const int MaxResourceGroupDescriptionLength = 500;
    public const int ResourcesQueryLimit = 1000;

    public const string DefaultResourceTypeName = "default";
    public const string DefaultResourceStatusName = "new";

    public const string SourceName = "ResourcesDataService";
    public const string SourceVersion = "1.0";
    /// <summary>
    /// TraceId http headers
    /// </summary>
    public const string TraceIdHeader = "X-five0-traceid";
    /// <summary>
    /// Compose a New request unique id
    /// SourceName + Guid
    /// </summary>
    /// <returns></returns>
    public static string GetNewTraceId() => $"{SourceName}:{Guid.NewGuid()}";

    /// <summary>
    /// Create a new PK using Guid version 7
    /// </summary>
    /// <returns></returns>
    public static Guid CreatePK(DateTimeOffset? seed = null) => Guid.CreateVersion7(seed ?? DateTimeOffset.UtcNow);

    /// <summary>
    /// Normalize string input
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string? ToNormalForm(this string? input) =>
        input?.Trim().ToLower();

    /// <summary>
    /// Normalize string input or return default value
    /// </summary>
    /// <param name="input"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static string ToNormalFormOrDefault(this string? input, string defaultValue) =>
        string.IsNullOrWhiteSpace(input?.Trim()) ? defaultValue : input.ToNormalForm()!;    
}