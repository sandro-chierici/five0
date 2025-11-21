namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Data Model
/// Id is the primary key in the database
/// Resource URN (unique reference name) is the unique identifier for the resource for external World
/// composed by (TenantCode, ResourceCode) joined by colon (:) ex. "TenantA:Resource123"
/// </summary>
[Table("resource")]
public class Resource
{
    [Column("resource_id")]
    public long ResourceId { get; set; }

    [Column("resource_code")]
    public string ResourceCode { get; set; }    

    [Column("tenant_code")]
    public string TenantCode { get; set; }

    [Column("resourcetype_id")]
    public long? ResourceTypeId { get; set; }
    /// <summary>
    /// Mnemonic name of the Resource
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
