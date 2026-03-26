using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Reporting.Persistence.Documents;

public sealed class ReportRecordDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Rsc { get; set; } = string.Empty;
    public string OrisCode { get; set; } = string.Empty;
    public string? Discipline { get; set; }
    public int Version { get; set; }
    public string DataHash { get; set; } = string.Empty;
    public string S3Key { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
