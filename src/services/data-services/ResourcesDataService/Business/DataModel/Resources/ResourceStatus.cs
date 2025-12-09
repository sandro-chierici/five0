
using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Status
/// </summary>
[Table("resource_status")]
public class ResourceStatus
{
    [Column("resourcestatus_id")]
    public required Guid ResourceStatusId { get; set; }

    [Column("tenant_id")]
    public required Guid TenantId { get; set; }

    [Column("status_code")]
    public string? StatusCode { get; set; }

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

