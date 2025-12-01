using System.ComponentModel.DataAnnotations.Schema;

namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resources hierarchy
/// </summary>
[Table("resource_tree")]
public class ResourceTree
{
    /// <summary>
    /// Not exposed autoincremented primary key
    /// </summary>
    [Column("resourcetree_id")]
    public long ResourceTreeId { get; set; }

    [Column("tenant_id")]
    public required Guid TenantId { get; set; }

    [Column("parent_resource_id")]
    public required Guid ParentResourceId { get; set; }

    [Column("child_resource_id")]
    public required Guid ChildResourceId { get; set; }

    [Column("utc_created")]
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
