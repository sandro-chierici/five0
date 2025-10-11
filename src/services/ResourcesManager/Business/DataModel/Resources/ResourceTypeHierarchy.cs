namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
public class ResourceTypeHierarchy
{
    public int Id { get; set; }
    public long TenantId { get; set; }
    public int ResourceTypeParentId { get; set; }
    public int ResourceTypeChildId { get; set; }
  
}
