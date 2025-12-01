using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource Group
/// </summary>
[Table("resource_group")]
public class ResourceGroup
{
    [Column("resourcegroup_id")]
    public required Guid ResourceGroupId { get; set; }

    [Column("tenant_id")]
    public required Guid TenantId { get; set; }

    [Column("resourcegroup_code")]
    public required string ResourceGroupCode { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
    
    [Column("parent_resourcegroup_id")]
    public Guid? ParentResourceGroupId { get; set; }
    public bool IsRootGroup() => ParentResourceGroupId == null;
}
