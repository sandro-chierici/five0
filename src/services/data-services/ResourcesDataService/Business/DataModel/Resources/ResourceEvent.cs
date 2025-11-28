using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

// History class for tracking status changes
[Table("resource_event")]
public class ResourceEvent
{
    /// <summary>
    /// Here the key is autoincremented long because we do not need to expose this Id outside of the system
    /// </summary>
    [Column("resourceevent_id")]
    public long ResourceEventId { get; set; }
    [Column("tenant_id")]
    public required string TenantId { get; set; }
    [Column("resource_id")]
    public required string ResourceId { get; set; }
    [Column("resourcestatus_id")]
    public required string ResourceStatusId { get; set; }
    [Column("utc_event")]
    public DateTimeOffset? UtcEvent { get; set; } = DateTimeOffset.UtcNow;
    [Column("message")]
    public string? Message  { get; set; }
    [Column("metadata")]
    public string? Metadata  { get; set; }
}