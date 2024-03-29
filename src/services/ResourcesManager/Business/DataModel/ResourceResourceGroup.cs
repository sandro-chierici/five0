﻿namespace ResourcesManager.Business.DataModel;

public class ResourceResourceGroup
{
    public long Id { get; set; }
    public long ResourceId { get; set; }
    public long ResourceGroupId { get; set; }
    public DateTimeOffset? CreatedDateUtc { get; set; } = DateTimeOffset.UtcNow;

    public ResourceResourceGroup() { }
}
