using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataModel.Tenants;

namespace ResourcesManager.Business.DataViews;

/// <summary>
/// Resource def
/// </summary>
public class ResourceView
{
    public required Resource Resource { get; init; } 
    public Tenant? Tenant { get; init; }
    public ResourceType? ResourceType { get; init; }
    public ResourceStatus? CurrentStatus { get; init; }
    public List<ResourceGroup> ResourceGroups { get; init; } = [];
}
