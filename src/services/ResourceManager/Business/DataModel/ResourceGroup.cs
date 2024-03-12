namespace ResourceManager.Business.DataModel;

public class ResourceGroup
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreatedDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public ResourceGroup() { }
}
