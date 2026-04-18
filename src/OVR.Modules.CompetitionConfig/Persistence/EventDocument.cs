using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class EventDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string? Modifier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? Size { get; set; }
    public List<PhaseSubDocument> Phases { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StructureGeneratedAt { get; set; }
}

public sealed class PhaseSubDocument
{
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public int UnitCount { get; set; }
}
