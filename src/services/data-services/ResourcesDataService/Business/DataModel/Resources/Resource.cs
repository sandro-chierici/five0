using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Data Model
/// Id is the primary key in the database
/// Resource URN (unique reference name) is the unique identifier for the resource for external World
/// uniqueness consists of TenantId + ResourceCode
/// </summary>
[Table("resource")]
public class Resource
{
    /// <summary>
    /// Database primary key
    /// </summary>
    [Column("resource_id")]
    public required Guid ResourceId { get; set; } 
    /// <summary>
    /// Tenant 
    /// </summary>
    [Column("tenant_id")]
    public required Guid TenantId { get; set; }
    /// <summary>
    /// Resource Type Id        
    /// </summary>
    [Column("resourcetype_id")]
    public Guid? ResourceTypeId { get; set; }    
    /// <summary>
    /// Tenant scoped unique code of the Resource 
    /// </summary>
    [Column("resource_code")]
    public required string ResourceCode { get; set; }    
    /// <summary>
    /// Mnemonic name of the Resource 
    /// (searchable, not unique)
    /// </summary>
    [Column("name")]
    public string? Name { get; set; }
    /// <summary>
    /// Mnemonic name of the Resource
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }    
    /// <summary>
    /// Resource Metadata in JSON format
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
