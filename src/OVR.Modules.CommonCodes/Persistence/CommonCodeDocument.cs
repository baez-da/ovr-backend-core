using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.CommonCodes.Persistence;

public sealed class CommonCodeDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public Dictionary<string, LocalizedTextDocument> Name { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
}

public sealed class LocalizedTextDocument
{
    public string Long { get; set; } = string.Empty;
    public string? Short { get; set; }
}
