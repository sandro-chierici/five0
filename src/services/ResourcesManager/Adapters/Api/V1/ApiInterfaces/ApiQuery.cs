namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;

/// <summary>
/// Define a query over resources
/// </summary>
public class ApiQuery()
{
    /// <summary>
    /// Search with a list of ids
    /// </summary>
    public long[]? Id { get; set; }
    /// <summary>
    /// Name startsWith
    /// </summary>
    public string? StartsWith { get; set; }
    /// <summary>
    /// Resource has typeId
    /// </summary>
    public long[]? ResourceTypeId { get; set; }
    /// <summary>
    /// resource is in the group
    /// </summary>
    public long[]? ResourceGroupId { get; set; }
    /// <summary>
    /// by status
    /// </summary>
    public long[]? ResourceStatusId { get; set; }

}