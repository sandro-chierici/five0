namespace ResourcesManager.Business.DataModel.Tenants;

public class TenantHierarchy
{
    public long Id { get; set; }
    public long TenantParentId { get; set; }
    public long TenantChildId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

