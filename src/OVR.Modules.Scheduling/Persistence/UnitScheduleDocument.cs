using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class UnitScheduleDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string EventRsc { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int OrderInSession { get; set; }
    public int OrderInLocation { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
