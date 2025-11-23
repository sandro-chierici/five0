namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;

/// <summary>
/// Request to create a resource
/// </summary>
public class CreateResourceRequest
{
    public string? ResourceCode { get; set; }
    public string? TenantCode { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ResourceTypeCode { get; set; }
    public string? ResourceGroupCode { get; set; }
    public object? Metadata { get; set; }
}

/// <summary>
/// Request to create a resource
/// </summary>
public class CreateResourceGroupRequest
{
    public string? ResourceGroupCode { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentResourceGroupCode { get; set; }
    public object? Metadata { get; set; }
}