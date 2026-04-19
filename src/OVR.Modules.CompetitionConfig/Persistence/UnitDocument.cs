using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class UnitDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string EventRsc { get; set; } = string.Empty;
    public string PhaseCode { get; set; } = string.Empty;
    public int UnitNumber { get; set; }
    public int? SeedA { get; set; }
    public int? SeedB { get; set; }
    public DateTime CreatedAt { get; set; }
}
