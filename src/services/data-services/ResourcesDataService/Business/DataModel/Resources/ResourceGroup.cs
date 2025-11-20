namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Group
/// Entity key is tuple (Tenant, Code, Org)
/// </summary>
public class ResourceGroup
{
    public long Id { get; set; }
    public string Tenant { get; set; }
    public string Code { get; set; }
    public string Org { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
     public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
