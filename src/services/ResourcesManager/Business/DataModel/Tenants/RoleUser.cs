namespace ResourcesManager.Business.DataModel.Tenants;

public class RoleUser
{
    public long Id { get; set; }
    public long? TenantId { get; set; }    
    public long RoleId { get; set; }
    public long UserId { get; set; }
}