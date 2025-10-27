﻿namespace ResourcesManager.Adapters.Api.V1.ApiInterfaces;

/// <summary>
/// Request to create a resource
/// </summary>
public class CreateResourceRequest
{
    public string? Name { get; set; }
    public long? TenantId { get; set; }
    public string? Description { get; set; }
    public long? ResourceTypeId { get; set; }
    public long? ResourceGroupId { get; set; }
}
