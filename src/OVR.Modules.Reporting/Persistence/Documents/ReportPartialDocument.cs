using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Reporting.Persistence.Documents;

public sealed class ReportPartialDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
