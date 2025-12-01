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

    public static Guid GetNewPK() => Guid.CreateVersion7(DateTimeOffset.UtcNow);

    public static string GetResourceUrn(string tenantId, string resourceCode) =>
        $"urn:five0:resource:{tenantId}:{resourceCode}";
    
    public static string GetResourceGroupUrn(string tenantId, string resourceGroupCode) =>
        $"urn:five0:resourcegroup:{tenantId}:{resourceGroupCode}";

    public static string GetResourceTypeUrn(string tenantId, string resourceTypeCode) =>
        $"urn:five0:resourcetype:{tenantId}:{resourceTypeCode}";

    /// <summary>
    /// Normalize string input
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string? Normalized(this string? input) =>
        input?.Trim().ToLower();

    public static string NormalizedAndDefault(this string? input, string defaultValue = "") =>
        string.IsNullOrWhiteSpace(input) ? defaultValue : input.Normalized()!;    
}