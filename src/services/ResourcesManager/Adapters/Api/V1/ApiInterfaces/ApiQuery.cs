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

    public string? StartsWith { get; set; }

    public long[]? ResourceTypeId { get; set; }

    public long[]? ResourceGroupId { get; set; }

}