using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Type Data Model
/// </summary>
[Table("resource_type")]
public class ResourceType
{
    /// <summary>
    /// Database Primary key
    /// </summary>
    [Column("resourcetype_id")]
    public required Guid ResourceTypeId { get; set; }
    /// <summary>
    /// Tenant Code part of the Entity primary key
    /// </summary>
    [Column("tenant_id")]
    public required Guid TenantId { get; set; }
    /// <summary>
    /// Code part of the Entity primary key
    /// </summary>
    [Column("type_code")]
    public required string TypeCode { get; set; }
    /// <summary>
    /// Mnemonic description of the Resource Type
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }
    /// <summary>
    /// Resource Type Metadata in JSON format
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }
    /// <summary>
    /// Parent Resource Type Id
    /// </summary>
    [Column("parent_resourcetype_id")]
    public Guid? ParentResourceTypeId { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;

    public bool IsRootType() => ParentResourceTypeId == null;
}
