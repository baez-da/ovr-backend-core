using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal sealed class MongoUnitRepository(IMongoDatabase database) : IUnitRepository
{
    private IMongoCollection<UnitDocument> Collection =>
        database.GetCollection<UnitDocument>("competitionconfig_units");

    public async Task<Unit?> GetByRscAsync(string unitRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Unit>> ListByEventAsync(string eventRsc, CancellationToken ct = default)
    {
        var docs = await Collection
            .Find(d => d.EventRsc == eventRsc)
            .SortBy(d => d.PhaseCode).ThenBy(d => d.UnitNumber)
            .ToListAsync(ct);
        return docs.Select(UnitMapping.ToDomain).ToList();
    }

    public async Task AddManyAsync(IEnumerable<Unit> units, CancellationToken ct = default)
    {
        var docs = units.Select(UnitMapping.ToDocument).ToList();
        if (docs.Count == 0) return;
        await Collection.InsertManyAsync(
            docs,
            new InsertManyOptions { IsOrdered = false },
            ct);
    }
}
