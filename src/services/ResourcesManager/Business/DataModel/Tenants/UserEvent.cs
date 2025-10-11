namespace ResourcesManager.Business.DataModel.Tenants;

public class UserEvents
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long UserId { get; set; }
    public DateTimeOffset UtcEventTime { get; set; } 
    public string? EventType { get; set; }
    public string? EventData { get; set; }
}