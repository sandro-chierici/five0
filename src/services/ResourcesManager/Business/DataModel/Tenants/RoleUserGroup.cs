namespace ResourcesManager.Business.DataModel.Tenants;

public class RoleUserGroup
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public long RoleId { get; set; }
    public long UserGroupId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}