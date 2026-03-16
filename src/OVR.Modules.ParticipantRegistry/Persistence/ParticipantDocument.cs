using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.ParticipantRegistry.Persistence;

public sealed class ParticipantDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string GenderCode { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string Noc { get; set; } = string.Empty;
    public string PrintName { get; set; } = string.Empty;
    public string TvName { get; set; } = string.Empty;
    public Dictionary<string, string> ExtendedDescription { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
