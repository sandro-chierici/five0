namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceResourceGroup
{
    public long Id { get; set; }
    public long TenantId { get; set; }    
    public long ResourceId { get; set; }
    public long ResourceGroupId { get; set; }
    public DateTimeOffset? CreatedDateUtc { get; set; } = DateTimeOffset.UtcNow;

}
