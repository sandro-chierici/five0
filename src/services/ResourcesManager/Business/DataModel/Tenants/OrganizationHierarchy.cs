namespace ResourcesManager.Business.DataModel.Tenants;

public class OrganizationHierarchy
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long OrganizationParentId { get; set; }
    public long OrganizationChildId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

