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
}
