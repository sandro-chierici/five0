namespace ResourcesManager.Business.DataModel.Tenants;

public class User
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public string? Username { get; set; }
    public string? Code { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Description { get; set; } 
    public string? Email { get; set; }
    public string? Phone { get; set; }
}