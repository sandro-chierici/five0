namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
public class ResourceTypeHierarchy
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ResourceTypeParentId { get; set; }
    public long ResourceTypeChildId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
