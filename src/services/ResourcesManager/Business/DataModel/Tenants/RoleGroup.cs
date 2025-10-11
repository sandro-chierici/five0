namespace ResourcesManager.Business.DataModel.Tenants;

public class RoleGroup
{
    public long Id { get; set; }
    public long? TenantId { get; set; }    
    public long RoleId { get; set; }
    public long UserGroupId { get; set; }
}