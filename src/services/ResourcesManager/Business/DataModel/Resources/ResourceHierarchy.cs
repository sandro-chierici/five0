namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
public class ResourceHierarchy
{
    public long Id { get; set; }
    public long TenantId { get; set; }    
    public long ParentResourceId { get; set; }
    public long ChildResourceId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
