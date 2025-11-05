namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceType
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public string? HasTable { get; set; }
    public long? ResourceTypeParentId { get; set; }
    public bool IsRootType() => ResourceTypeParentId == null;
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
