namespace ResourcesManager.Business.DataModel.Tenants;

public class Tenant
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Alias { get; set; }
    public string? IdCode { get; set; }
    public string? Description { get; set;}
}

