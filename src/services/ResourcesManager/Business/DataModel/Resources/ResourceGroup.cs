namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceGroup
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
     public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
