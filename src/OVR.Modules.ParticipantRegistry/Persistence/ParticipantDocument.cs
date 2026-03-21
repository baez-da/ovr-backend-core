using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.ParticipantRegistry.Persistence;

public sealed class ParticipantDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public List<ParticipantFunctionDocument> Functions { get; set; } = [];
    public string? GivenName { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public string GenderCode { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string Organisation { get; set; } = string.Empty;
    public string PrintName { get; set; } = string.Empty;
    public string PrintInitialName { get; set; } = string.Empty;
    public string TvName { get; set; } = string.Empty;
    public string TvInitialName { get; set; } = string.Empty;
    public string TvFamilyName { get; set; } = string.Empty;
    public string PscbName { get; set; } = string.Empty;
    public string PscbShortName { get; set; } = string.Empty;
    public string PscbLongName { get; set; } = string.Empty;
    public Dictionary<string, string> ExtendedDescription { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class ParticipantFunctionDocument
{
    public string FunctionId { get; set; } = string.Empty;
    public string DisciplineCode { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}
