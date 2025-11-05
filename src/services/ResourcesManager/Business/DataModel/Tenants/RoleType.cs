namespace ResourcesManager.Business.DataModel.Tenants;

public class RoleType
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public string? FactoryCode { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}