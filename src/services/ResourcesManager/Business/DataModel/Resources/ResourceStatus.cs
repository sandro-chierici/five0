
namespace ResourcesManager.Business.DataModel.Resources;

// Enumerative class for possible statuses
public class ResourceStatus
{
    public long Id { get; set; }
    public long? TenantId { get; set; }    
    public string? Name { get; set; }
    public string? Description { get; set; }
}

