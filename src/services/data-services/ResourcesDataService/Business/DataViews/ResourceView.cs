namespace ResourcesManager.Business.DataViews;

public record struct ResourceTypeView()
{
    public string? ResourceTypeCode { get; init; }
    public string? Description { get; init; }
    public bool IsRootType { get; init; }
}

public record struct ResourceStatusView(string? ResourceStatusCode, string? Description);

public record struct ResourceGroupView(string? ResourceGroupCode, string? Description);

/// <summary>
/// Resource def
/// </summary>
public record ResourceView()
{
    public string? ResourceCode { get; set; }
    public string? TenantId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public ResourceTypeView? ResourceType { get; init; }
    public DateTimeOffset? UtcCreated { get; init; }
    public ResourceStatusView? CurrentStatus { get; init; }
    public List<ResourceGroupView>? ResourceGroups { get; set; }
    public object? Metadata { get; init; }
}
