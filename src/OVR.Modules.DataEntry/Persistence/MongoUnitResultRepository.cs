using MongoDB.Driver;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class MongoUnitResultRepository : IUnitResultRepository
{
    private readonly IMongoCollection<UnitResultDocument> _collection;

    public MongoUnitResultRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UnitResultDocument>("unitResults");
    }

    public async Task<bool> ExistsAsync(string unitRsc, CancellationToken ct)
        => await _collection.Find(d => d.Id == unitRsc).Limit(1).AnyAsync(ct);

    public async Task<UnitResult?> GetAsync(string unitRsc, CancellationToken ct)
    {
        var doc = await _collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitResultMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<UnitResult>> GetManyAsync(
        IReadOnlyList<string> unitRscs, CancellationToken ct)
    {
        if (unitRscs.Count == 0) return Array.Empty<UnitResult>();
        var docs = await _collection.Find(d => unitRscs.Contains(d.Id)).ToListAsync(ct);
        return docs.Select(UnitResultMapping.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UnitResult>> ListAllAsync(CancellationToken ct)
    {
        var docs = await _collection.Find(Builders<UnitResultDocument>.Filter.Empty).ToListAsync(ct);
        return docs.Select(UnitResultMapping.ToDomain).ToList();
    }

    public async Task SaveNewAsync(UnitResult unitResult, CancellationToken ct)
    {
        try
        {
            var doc = UnitResultMapping.ToDocument(unitResult);
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (
            ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Idempotent: another event instance created it concurrently. No-op.
        }
    }

    public async Task UpdateAsync(UnitResult unitResult, CancellationToken ct)
    {
        var doc = UnitResultMapping.ToDocument(unitResult);
        await _collection.ReplaceOneAsync(
            d => d.Id == doc.Id, doc,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken: ct);
    }
}
