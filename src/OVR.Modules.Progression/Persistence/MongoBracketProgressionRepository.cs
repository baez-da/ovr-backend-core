using MongoDB.Driver;
using OVR.Modules.Progression.Domain;

namespace OVR.Modules.Progression.Persistence;

public sealed class MongoBracketProgressionRepository(IMongoDatabase db)
    : IBracketProgressionRepository
{
    public const string CollectionName = "progression_brackets";

    private readonly IMongoCollection<BracketProgressionDocument> _collection =
        db.GetCollection<BracketProgressionDocument>(CollectionName);

    public async Task<BracketProgression?> GetByEventAsync(string eventRsc, CancellationToken ct)
    {
        var doc = await _collection
            .Find(d => d.EventRsc == eventRsc)
            .FirstOrDefaultAsync(ct);
        return doc is null ? null : BracketProgressionMapping.ToDomain(doc);
    }

    public async Task<bool> SaveNewAsync(BracketProgression agg, CancellationToken ct)
    {
        var doc = BracketProgressionMapping.ToDocument(agg);
        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task ReplaceAsync(BracketProgression agg, CancellationToken ct)
    {
        var doc = BracketProgressionMapping.ToDocument(agg);
        await _collection.ReplaceOneAsync(
            d => d.EventRsc == agg.EventRsc,
            doc,
            cancellationToken: ct);
    }
}
