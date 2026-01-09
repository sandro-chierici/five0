namespace ResourcesManager.Business.DataViews;

public record struct ResourceTypeView()
{
    public string? ResourceTypeId { get; init; }
    public string? TypeCode { get; init; }
    public string? Description { get; init; }
    public bool IsRootType { get; init; }
    public object? ResourceTypeMetadata { get; set; }
}

public record struct ResourceStatusView(string? ResourceStatusId, string? StatusCode, string? Description, DateTimeOffset? UtcStatus)
{
    public object? ResourceStatusMetadata { get; set; }
};

public record struct ResourceGroupView(string? ResourceGroupId, string? GroupCode, string? Description)
{
    public bool IsRootGroup { get; set; }
    public object? ResourceGroupMetadata { get; set; } 
};

/// <summary>
/// Resource def
/// </summary>
public record struct ResourceView()
{
    public string? ResourceId { get; init; }
    public string? ResourceCode { get; init; }
    public string? TenantId { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ResourceTypeView? ResourceType { get; init; }
    public DateTimeOffset? UtcCreated { get; init; }
    public IEnumerable<ResourceStatusView>? ResourceStatus { get; init; }
    public IEnumerable<ResourceGroupView>? ResourceGroups { get; init; }
    public object? ResourceMetadata { get; set; }
}
