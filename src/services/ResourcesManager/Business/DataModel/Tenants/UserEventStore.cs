namespace ResourcesManager.Business.DataModel.Tenants;

public class UserEventStore
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long UserId { get; set; }
    public long UserStatusId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}