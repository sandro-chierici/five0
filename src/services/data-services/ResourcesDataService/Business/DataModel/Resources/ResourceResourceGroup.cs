namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceResourceGroup
{
    public long Id { get; set; }
    public string Tenant { get; set; }
    public string Org { get; set; }
    public string ResourceCode  { get; set; }
    public string ResourceGroupCode { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
