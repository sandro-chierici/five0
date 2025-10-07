using System;

namespace ResourcesManager.Business.DataModel;

// Enumerative class for possible statuses
public class ResourceStatus
{
    public int Id { get; set; }
    public long TenantId { get; set; }    
    public required string Name { get; init; }
    public string? Description { get; set; }
}

// History class for tracking status changes
public class ResourceStatusHistory
{
    public int Id { get; set; }
    public long TenantId { get; set; }
    // Foreign key to Resource
    public long ResourceId { get; set; }

    // Foreign key to ResourceStatusEnum
    public int ResourceStatusEnumId { get; set; }

    public DateTime EnteredDate { get; set; }
    
    public string? Notes { get; set; }
}
