using ResourcesManager.Business.DataModel.Resources;
using ResourcesManager.Business.DataModel.Tenants;

namespace ResourcesManager.Business.DataViews;

/// <summary>
/// Resource def
/// </summary>
public class ResourceView
{
    public required Resource Resource { get; set; } 
    public ResourceType? ResourceType { get; set; }
    public ResourceStatus? CurrentStatus { get; set; }
    public List<ResourceGroup> ResourceGroups { get; set; } = [];
}
