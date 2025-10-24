namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceType
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }

}
