namespace ResourcesManager.Business.Application.ExternalServices.CustomService;

public interface ICustomService
{
    // CRUD operations matching /api/v1/resources endpoints
    ValueTask<OkOrError<ResourceDto>> CreateResourceAsync(CreateResourceRequest request);
    ValueTask<OkOrError<ResourceDto>> GetResourceAsync(string resourceId);
    ValueTask<OkOrError<ResourceListResponse>> ListResourcesAsync();
    ValueTask<OkOrError<ResourceDto>> UpdateResourceAsync(string resourceId, UpdateResourceRequest request);
    ValueTask<OkOrError<bool>> DeleteResourceAsync(string resourceId);  
}

// DTOs matching CustomService models
public class ResourceDto
{
    public required string Id { get; set; }
    public required Dictionary<string, object?> Specs { get; set; }
}

public class CreateResourceRequest
{
    public required Dictionary<string, object?> Specs { get; set; }
}

public class UpdateResourceRequest
{
    public required Dictionary<string, object?> Specs { get; set; }
}

public class ResourceListResponse
{
    public required List<ResourceDto> Resources { get; set; }
    public int Count { get; set; }
}

