using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

[Table("resource_to_group")]
public class ResourceToGroup
{
    [Column("resourcetogroup_id")]
    public long ResourceToGroupId { get; set; }

    [Column("tenant_id")]
    public required Guid TenantId { get; set; }

    [Column("resource_id")]
    public required Guid ResourceId  { get; set; }

    [Column("resourcegroup_id")]
    public required Guid ResourceGroupId { get; set; }
    
    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
