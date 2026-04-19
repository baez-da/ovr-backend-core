using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Progression.Persistence;

public sealed class BracketProgressionDocument
{
    [BsonId]
    public required string EventRsc { get; set; }

    public required List<ProgressionEdgeDocument> Edges { get; set; } = [];
    public required List<string> ReadyTargets { get; set; } = [];
    public required List<PendingAdvancementDocument> PendingAdvancements { get; set; } = [];
    public required DateTime CreatedAt { get; set; }
}

public sealed class ProgressionEdgeDocument
{
    public required string SourceUnitRsc { get; set; }
    public required string Outcome { get; set; }
    public required string TargetUnitRsc { get; set; }
    public required int TargetSlot { get; set; }
}

public sealed class PendingAdvancementDocument
{
    public required string TargetUnitRsc { get; set; }
    public required int TargetSlot { get; set; }
    public required string ParticipantId { get; set; }
    public required string SourceUnitRsc { get; set; }
    public required DateTime RecordedAt { get; set; }
}
