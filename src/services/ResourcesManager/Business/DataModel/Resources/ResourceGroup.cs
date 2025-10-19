namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceGroup
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? CreatedDateUtc { get; set; }
}
