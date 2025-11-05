using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ResourcesManager.Business.DataModel.Tenants;

public class Organization
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationTypeId { get; set; }
    public string? Code { get; set; }
    public string? Alias { get; set; }
    public string? Description { get; set; }
    public string? HasTable { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

