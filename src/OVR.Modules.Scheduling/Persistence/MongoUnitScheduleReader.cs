using MongoDB.Driver;
using OVR.Modules.Scheduling.Contracts;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class MongoUnitScheduleReader : IUnitScheduleReader
{
    private readonly IMongoCollection<UnitScheduleDocument> _schedules;

    public MongoUnitScheduleReader(IMongoDatabase database)
    {
        _schedules = database.GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");
    }

    public async Task<IReadOnlyList<string>> ListUnitRscs(
        string? sessionCode, string? locationCode, CancellationToken ct)
    {
        var filter = Builders<UnitScheduleDocument>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(sessionCode))
            filter &= Builders<UnitScheduleDocument>.Filter.Eq(d => d.SessionCode, sessionCode);
        if (!string.IsNullOrWhiteSpace(locationCode))
            filter &= Builders<UnitScheduleDocument>.Filter.Eq(d => d.LocationCode, locationCode);

        var docs = await _schedules.Find(filter).Project(d => d.Id).ToListAsync(ct);
        return docs;
    }
}
