
namespace ResourcesManager.Business.DataModel.Resources;

// Enumerative class for possible statuses
public class ResourceStatus
{
    public long Id { get; set; }
    public long TenantId { get; set; }    
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

