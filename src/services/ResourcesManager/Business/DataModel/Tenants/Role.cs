namespace ResourcesManager.Business.DataModel.Tenants;

public class Role
{
    public long Id { get; set; }
    public long? TenantId { get; set; }    
    public string? RoleName { get; set; }
    public string? Alias { get; set; }
    public string? FactoryCode { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}