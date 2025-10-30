namespace ResourcesManager.Business.DataModel.Tenants;

public class UserUserGroup
{
    public long Id { get; set; }
    public long TenantId { get; set; }    
    public long UserGroupId { get; set; }
    public long UserId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}