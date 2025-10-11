namespace ResourcesManager.Business.DataModel.Tenants;

public class TenantTree
{
    public long Id { get; set; }
    public long TenantParentId { get; set; }
    public long TenantChildId { get; set; }
}

