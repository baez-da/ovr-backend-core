using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.OfficialAssignment.Persistence;

public sealed class OfficialAssignmentDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string UnitRsc { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public string? AccreditationFunction { get; set; }
    public string Organisation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TeamId { get; set; }
    public string? ExternalSystem { get; set; }
    public string? ExternalIdValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
