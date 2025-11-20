namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Type def
/// Entity key is tuplate (TenantId, Code, OrganizationId)
/// </summary>
public class ResourceType
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
    /// Mnemonic name of the Resource Type
    /// </summary>
    public string? Name { get; set; }
    public string? Description { get; set; }
    /// <summary>
    /// Resource Type Metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }
    /// <summary>
    /// Parent Resource Type Code
    /// </summary>
    public string? ResourceTypeParentCode { get; set; }
    public bool IsRootType() => ResourceTypeParentCode == null;
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
