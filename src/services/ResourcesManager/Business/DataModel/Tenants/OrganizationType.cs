namespace ResourcesManager.Business.DataModel.Tenants;

public class OrganizationType
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string? Alias { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

