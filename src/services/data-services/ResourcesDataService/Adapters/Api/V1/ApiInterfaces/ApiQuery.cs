namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;

/// <summary>
/// Define a query over resources
/// </summary>
public class ApiQuery()
{
    /// <summary>
    /// Search with a list of Resource codes
    /// </summary>
    public string[]? Ids { get; set; }
    /// <summary>
    /// Code startsWith
    /// </summary>
    public string? StartsWith { get; set; }
    /// <summary>
    /// Resource has typeId
    /// </summary>
    public string[]? ResourceTypeId { get; set; }
    /// <summary>
    /// resource is in the group
    /// </summary>
    public string[]? ResourceGroupId { get; set; }
    /// <summary>
    /// by status
    /// </summary>
    public string[]? ResourceStatusId { get; set; }

}