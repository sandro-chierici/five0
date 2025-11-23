using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

// History class for tracking status changes
[Table("resource_event")]
public class ResourceEvent
{
    [Column("resourceevent_id")]
    public long ResourceEventId { get; set; }
    [Column("tenant_code")]
    public required string TenantCode { get; set; }
    [Column("resource_id")]
    public long ResourceId { get; set; }
    [Column("resourcestatus_id")]
    public long ResourceStatusId { get; set; }
    [Column("utc_event")]
    public DateTimeOffset? UtcEvent { get; set; } = DateTimeOffset.UtcNow;
    [Column("message")]
    public string? Message  { get; set; }
    [Column("metadata")]
    public string? Metadata  { get; set; }
}