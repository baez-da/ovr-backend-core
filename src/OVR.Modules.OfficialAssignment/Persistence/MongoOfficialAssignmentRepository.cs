using System.Text.RegularExpressions;
using MongoDB.Driver;
using OVR.Modules.OfficialAssignment.Domain;

namespace OVR.Modules.OfficialAssignment.Persistence;

internal sealed class MongoOfficialAssignmentRepository(IMongoDatabase database) : IOfficialAssignmentRepository
{
    private IMongoCollection<OfficialAssignmentDocument> Collection =>
        database.GetCollection<OfficialAssignmentDocument>("officialAssignment_assignments");

    public async Task<OfficialAssignmentEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : OfficialAssignmentMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<OfficialAssignmentEntity>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default)
    {
        var filter = Builders<OfficialAssignmentDocument>.Filter.Regex(
            d => d.UnitRsc,
            new MongoDB.Bson.BsonRegularExpression($"^{Regex.Escape(rscPrefix)}"));
        var docs = await Collection.Find(filter).ToListAsync(ct);
        return docs.Select(OfficialAssignmentMapping.ToDomain).ToList();
    }

    public async Task AddAsync(OfficialAssignmentEntity assignment, CancellationToken ct = default)
    {
        var doc = OfficialAssignmentMapping.ToDocument(assignment);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(OfficialAssignmentEntity assignment, CancellationToken ct = default)
    {
        var doc = OfficialAssignmentMapping.ToDocument(assignment);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }
}
