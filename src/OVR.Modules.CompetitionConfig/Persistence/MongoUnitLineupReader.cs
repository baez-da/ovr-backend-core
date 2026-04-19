using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Contracts;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class MongoUnitLineupReader : IUnitLineupReader
{
    private readonly IMongoCollection<UnitDocument> _units;

    public MongoUnitLineupReader(IMongoDatabase database)
    {
        _units = database.GetCollection<UnitDocument>("competitionconfig_units");
    }

    public async Task<(int? SeedA, int? SeedB)> GetSeedsForUnit(
        string unitRsc, CancellationToken ct)
    {
        var doc = await _units
            .Find(u => u.Id == unitRsc)
            .Project(u => new { u.SeedA, u.SeedB })
            .FirstOrDefaultAsync(ct);

        return doc is null ? (null, null) : (doc.SeedA, doc.SeedB);
    }
}
