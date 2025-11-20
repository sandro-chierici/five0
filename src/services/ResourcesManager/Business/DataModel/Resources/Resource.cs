namespace ResourcesManager.Business.DataModel.Resources;

/// <summary>
/// Resource def
/// </summary>
public class Resource
{
    /// <summary>
    /// Database Primary key
    /// </summary>
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long? OrganizationId { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public long? ResourceTypeId { get; set; }
    public DateTimeOffset? UtcCreated { get; set; } = DateTimeOffset.UtcNow;
}
