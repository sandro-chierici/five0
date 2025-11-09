namespace ResourcesManager.Business.DataModel.Tenants;

public class UserType
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public string? Code { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}