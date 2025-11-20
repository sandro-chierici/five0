namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource def
/// Entity key is tuplate (Tenant, Code, Org)
/// </summary>
public class Resource
{
    /// <summary>
    /// Database Primary key
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// Tenant Id part of the Entity primary key
    /// </summary>
    public string Tenant { get; set; }
    /// <summary>
    /// Code part of the Entity primary key
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Organization Id part of the Entity primary key
    /// </summary>
    public string Org { get; set; }
    /// <summary>
    /// Resource Type Code
    /// The rest of the ResourceType key will be the same as Resource key (Tenant, Org) 
    /// </summary>
    public string? ResourceTypeCode { get; set; }
    /// <summary>
    /// Mnemonic name of the Resource
    /// </summary>
    public string? Name { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Resource Metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
