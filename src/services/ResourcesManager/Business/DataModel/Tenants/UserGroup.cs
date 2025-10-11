namespace ResourcesManager.Business.DataModel.Tenants;

public class UserGroup
{
    public long Id { get; set; }
    public long TenantId { get; set; }    
    public string? Name { get; set; }
    public string? Alias { get; set; }
    public string? FactoryCode { get; set; }
    public string? Description { get; set;}
}
