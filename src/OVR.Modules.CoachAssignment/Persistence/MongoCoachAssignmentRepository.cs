using System.Text.RegularExpressions;
using MongoDB.Driver;
using OVR.Modules.CoachAssignment.Domain;

namespace OVR.Modules.CoachAssignment.Persistence;

internal sealed class MongoCoachAssignmentRepository(IMongoDatabase database) : ICoachAssignmentRepository
{
    private IMongoCollection<CoachAssignmentDocument> Collection =>
        database.GetCollection<CoachAssignmentDocument>("coachAssignment_assignments");

    public async Task<CoachAssignmentEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : CoachAssignmentMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<CoachAssignmentEntity>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default)
    {
        var filter = Builders<CoachAssignmentDocument>.Filter.Regex(
            d => d.EventRsc,
            new MongoDB.Bson.BsonRegularExpression($"^{Regex.Escape(rscPrefix)}"));
        var docs = await Collection.Find(filter).ToListAsync(ct);
        return docs.Select(CoachAssignmentMapping.ToDomain).ToList();
    }

    public async Task AddAsync(CoachAssignmentEntity assignment, CancellationToken ct = default)
    {
        var doc = CoachAssignmentMapping.ToDocument(assignment);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(CoachAssignmentEntity assignment, CancellationToken ct = default)
    {
        var doc = CoachAssignmentMapping.ToDocument(assignment);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }
}
