namespace ResourcesManager.Business.DataModel.Resources;

// History class for tracking status changes
public class ResourceStatusStore
{
    public long Id { get; set; }
    public string Tenant { get; set; }
    public string Org { get; set; }
    public string ResourceCode { get; set; }
    public string ResourceStatusCode { get; set; }
    public DateTimeOffset? UtcEventDate { get; set; } = DateTimeOffset.UtcNow;
    public string? Message  { get; set; }
    public string? Metadata  { get; set; }
}