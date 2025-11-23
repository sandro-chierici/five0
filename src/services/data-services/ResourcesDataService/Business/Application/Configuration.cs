
namespace ResourcesManager.Business.Application.Configuration;

public class Five0Config
{
    public string? TimeServiceUrl { get; set; }
    public string? EventServiceUrl { get; set; }
}

// create a configuration class for ResourceDataService from appstettings.json
public class ResourceDataServiceConfig
{
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 500;
}