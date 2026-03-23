using MongoDB.Driver;
using OVR.Modules.CommonCodes.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CommonCodes.Persistence;

internal sealed class MongoCommonCodeRepository(IMongoDatabase database)
    : ICommonCodeRepository, ICommonCodeReader
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

    // ICommonCodeReader implementation
    async Task<MultilingualText?> ICommonCodeReader.GetNameAsync(string type, string code, CancellationToken ct)
    {
        var doc = await GetAsync(type, code, ct);
        if (doc is null) return null;

        var translations = doc.Name.ToDictionary(
            kvp => kvp.Key,
            kvp => LocalizedText.Create(kvp.Value.Long, kvp.Value.Short));
        return MultilingualText.Create(translations);
    }

    async Task<bool> ICommonCodeReader.ExistsAsync(string type, string code, CancellationToken ct)
        => await ExistsAsync(type, code, ct);
}
