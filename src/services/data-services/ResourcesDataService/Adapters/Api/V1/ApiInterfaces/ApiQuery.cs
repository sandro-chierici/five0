namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;

/// <summary>
/// Define a query over resources
/// </summary>
public class ApiQuery()
{
    /// <summary>
    /// Search with a list of Resource codes
    /// </summary>
    public string[]? ResourceCode { get; set; }
    /// <summary>
    /// Name startsWith
    /// </summary>
    public string? NameStartsWith { get; set; }
    /// <summary>
    /// Resource has typeId
    /// </summary>
    public string[]? ResourceTypeCode { get; set; }
    /// <summary>
    /// resource is in the group
    /// </summary>
    public string[]? ResourceGroupCode { get; set; }
    /// <summary>
    /// by status
    /// </summary>
    public string[]? ResourceStatusCode { get; set; }
}