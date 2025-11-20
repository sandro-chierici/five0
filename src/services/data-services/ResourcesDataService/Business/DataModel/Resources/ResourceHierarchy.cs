namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
public class ResourceHierarchy
{
    public long Id { get; set; }
    public string Tenant { get; set; }
    public string Org { get; set; }
    public string ParentResourceCode { get; set; }
    public string ChildResourceCode { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
