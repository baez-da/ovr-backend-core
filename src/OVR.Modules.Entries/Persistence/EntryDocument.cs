using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Entries.Persistence;

public sealed class EntryDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string EventRsc { get; set; } = string.Empty;
    public string CompetitorType { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InscriptionStatus { get; set; } = string.Empty;
    public string? RegisteredEventRsc { get; set; }
    public string? Category { get; set; }
    public string? TeamId { get; set; }
    public string? Seed { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalIdValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
