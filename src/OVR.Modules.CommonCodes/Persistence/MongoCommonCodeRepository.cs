using MongoDB.Driver;

namespace OVR.Modules.CommonCodes.Persistence;

internal sealed class MongoCommonCodeRepository(IMongoDatabase database)
    : ICommonCodeRepository
{
    private IMongoCollection<CommonCodeDocument> Collection =>
        database.GetCollection<CommonCodeDocument>("commonCodes_codes");

    public async Task<IReadOnlyList<CommonCodeDocument>> GetByTypeAsync(string type, CancellationToken ct)
    {
        return await Collection.Find(d => d.Type == type)
            .SortBy(d => d.Order)
            .ToListAsync(ct);
    }

    public async Task<CommonCodeDocument?> GetAsync(string type, string code, CancellationToken ct)
    {
        var id = $"{type}:{code}";
        return await Collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsAsync(string type, string code, CancellationToken ct)
    {
        var id = $"{type}:{code}";
        return await Collection.Find(d => d.Id == id).AnyAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetDistinctTypesAsync(CancellationToken ct)
    {
        var types = await Collection.DistinctAsync(d => d.Type, Builders<CommonCodeDocument>.Filter.Empty, cancellationToken: ct);
        return await types.ToListAsync(ct);
    }

    public async Task UpsertManyAsync(IReadOnlyList<CommonCodeDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0) return;

        var bulkOps = documents.Select(doc =>
            new ReplaceOneModel<CommonCodeDocument>(
                Builders<CommonCodeDocument>.Filter.Eq(d => d.Id, doc.Id), doc)
            { IsUpsert = true });

        await Collection.BulkWriteAsync(bulkOps, cancellationToken: ct);
    }

    public async Task DeleteByTypeAsync(string type, CancellationToken ct)
    {
        await Collection.DeleteManyAsync(d => d.Type == type, ct);
    }
}
