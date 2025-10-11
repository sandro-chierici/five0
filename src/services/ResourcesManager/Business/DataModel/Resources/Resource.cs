namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource def
/// </summary>
public class Resource
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public long? ResourceTypeId { get; set; }
}
