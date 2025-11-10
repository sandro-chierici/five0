namespace ResourcesManager.Business.DataViews;

public record struct ResourceTypeView()
{
    public string? Code { get; init; }
    public string? Description { get; init; }
    public long? ResourceTypeParentId { get; init; }
    public bool IsRootType { get; init; }
}

public record struct ResourceStatusView(string? Code, string? Description);

public record struct ResourceGroupView(string? Code, string? Description);

/// <summary>
/// Resource def
/// </summary>
public record ResourceView()
{
    public long Id { get; init; }
    public long TenantId { get; init; }
    public long? OrganizationId { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public ResourceTypeView? ResourceType { get; init; }
    public DateTimeOffset? UtcCreated { get; init; }
    public ResourceStatusView? CurrentStatus { get; init; }
    public List<ResourceGroupView>? ResourceGroups { get; set; }
    public object? Specs { get; init; }
}
