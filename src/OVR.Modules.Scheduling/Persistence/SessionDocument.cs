using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class SessionDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string VenueCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan? Leadin { get; set; }
    public DateTime CreatedAt { get; set; }
}
