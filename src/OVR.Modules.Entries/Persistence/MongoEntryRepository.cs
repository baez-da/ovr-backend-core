using System.Text.RegularExpressions;
using MongoDB.Driver;
using OVR.Modules.Entries.Domain;

namespace OVR.Modules.Entries.Persistence;

internal sealed class MongoEntryRepository(IMongoDatabase database) : IEntryRepository
{
    private IMongoCollection<EntryDocument> Collection =>
        database.GetCollection<EntryDocument>("entries_entries");

    public async Task<Entry?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : EntryMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Entry>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default)
    {
        var filter = Builders<EntryDocument>.Filter.Regex(
            d => d.EventRsc,
            new MongoDB.Bson.BsonRegularExpression($"^{Regex.Escape(rscPrefix)}"));
        var docs = await Collection.Find(filter).ToListAsync(ct);
        return docs.Select(EntryMapping.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Entry>> FindByParticipantIdAsync(string participantId, CancellationToken ct = default)
    {
        var docs = await Collection.Find(d => d.ParticipantId == participantId).ToListAsync(ct);
        return docs.Select(EntryMapping.ToDomain).ToList();
    }

    public async Task AddAsync(Entry entry, CancellationToken ct = default)
    {
        var doc = EntryMapping.ToDocument(entry);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Entry entry, CancellationToken ct = default)
    {
        var doc = EntryMapping.ToDocument(entry);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }

    public async Task DeleteByParticipantIdsAsync(
        IReadOnlyList<string> participantIds, string rscPrefix,
        CancellationToken ct = default)
    {
        if (participantIds.Count == 0) return;
        var filter = Builders<EntryDocument>.Filter.And(
            Builders<EntryDocument>.Filter.In(d => d.ParticipantId, participantIds),
            Builders<EntryDocument>.Filter.Regex(
                d => d.EventRsc,
                new MongoDB.Bson.BsonRegularExpression($"^{Regex.Escape(rscPrefix)}")));
        await Collection.DeleteManyAsync(filter, ct);
    }

    public async Task AddManyAsync(
        IReadOnlyList<Entry> entries, CancellationToken ct = default)
    {
        if (entries.Count == 0) return;
        var docs = entries.Select(EntryMapping.ToDocument).ToList();
        await Collection.InsertManyAsync(docs, cancellationToken: ct);
    }
}
