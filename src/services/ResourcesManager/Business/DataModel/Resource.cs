namespace ResourcesManager.Business.DataModel;

/// <summary>
/// Resource def
/// </summary>
public class Resource
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public string? Name { get; set; }

    public string? Description { get; set; }
    public int? ResourceTypeId { get; set; }
    public DateTimeOffset CreatedDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool? IsDisabled { get; set; }
    public DateTimeOffset? DisabledDateUtc { get; set; }
    public Resource() { }

}
