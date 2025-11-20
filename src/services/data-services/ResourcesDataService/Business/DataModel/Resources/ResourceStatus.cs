
namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Status
/// Entity key is tuple (Code, Tenant, Org) 
/// Could be tenant Wide (no Org)
/// </summary>
public class ResourceStatus
{
    /// <summary>
    /// Database Primary key
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Code part of the Entity primary key
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Tenant Id part of the Entity primary key
    /// </summary>  
    public string Tenant { get; set; }
    /// <summary>
    /// Organization Id part of the Entity primary key
    /// </summary>
    public string? Org { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Resource Status Metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

