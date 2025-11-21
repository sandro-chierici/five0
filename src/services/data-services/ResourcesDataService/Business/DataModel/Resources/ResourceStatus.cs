
namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Status
/// </summary>
[Table("resource_status")]
public class ResourceStatus
{
    /// <summary>
    /// Database Primary key
    /// </summary>
    [Column("resourcestatus_id")]
    public long ResourceStatusId { get; set; }

    [Column("tenant_code")]
    public string TenantCode { get; set; }

    [Column("resourcestatus_code")]
    public string ResourceStatusCode { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("description")] 
    public string? Description { get; set; }
    /// <summary>
    /// Resource Status Metadata in JSON format
    /// </summary>
    [Column("metadata")]    
    public string? Metadata { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}

