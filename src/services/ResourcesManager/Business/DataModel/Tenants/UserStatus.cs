namespace ResourcesManager.Business.DataModel.Tenants;

public class UserStatus
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}