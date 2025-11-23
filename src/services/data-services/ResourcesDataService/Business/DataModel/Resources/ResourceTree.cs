using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
[Table("resource_tree")]
public class ResourceTree
{
    [Column("resourcetree_id")]
    public long ResourceTreeId { get; set; }

    [Column("tenant_code")]
    public required string TenantCode { get; set; }

    [Column("parent_resource_id")]
    public long ParentResourceId { get; set; }

    [Column("child_resource_id")]
    public long ChildResourceId { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
