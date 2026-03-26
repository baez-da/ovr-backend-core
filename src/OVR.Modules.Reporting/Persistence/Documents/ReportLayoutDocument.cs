using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Reporting.Persistence.Documents;

public sealed class ReportLayoutDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Component { get; set; } = string.Empty;
    public string? Discipline { get; set; }
    public string? ChampionshipCode { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
