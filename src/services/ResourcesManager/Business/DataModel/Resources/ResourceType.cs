namespace ResourcesManager.Business.DataModel.Resources;

public class ResourceType
{
    public int Id { get; set; }
    public long TenantId { get; set; }

    public string? Name { get; set; }
    public string? Description { get; set; }

}
