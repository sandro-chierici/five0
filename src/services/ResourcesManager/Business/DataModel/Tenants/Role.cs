namespace ResourcesManager.Business.DataModel.Tenants;

public class Role
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public long? RoleTypeId { get; set; }
    public string? RoleName { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}