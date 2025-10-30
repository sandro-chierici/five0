namespace ResourcesManager.Business.DataModel.Resources;

// History class for tracking status changes
public class ResourceStatusHistory
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ResourceId { get; set; }
    public long ResourceStatusId { get; set; }
     public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}