namespace ResourcesManager.Business.DataModel.Resources;

[Table("resource_to_group")]
public class ResourceToGroup
{
    [Column("resourcetogroup_id")]
    public long ResourceToGroupId { get; set; }

    [Column("tenant_code")]
    public string TenantCode { get; set; }

    [Column("resource_id")]
    public long ResourceId  { get; set; }

    [Column("resourcegroup_id")]
    public long ResourceGroupId { get; set; }
    
    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
