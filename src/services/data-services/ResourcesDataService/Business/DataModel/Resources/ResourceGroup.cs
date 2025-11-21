namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Group
/// </summary>
[Table("resource_group")]
public class ResourceGroup
{
    [Column("resourcegroup_id")]
    public long ResourceGroupId { get; set; }

    [Column("tenant_code")]
    public string TenantCode { get; set; }

    [Column("resourcegroup_code")]
    public string ResourceGroupCode { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
