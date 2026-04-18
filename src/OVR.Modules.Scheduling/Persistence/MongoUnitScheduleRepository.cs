using MongoDB.Driver;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal sealed class MongoUnitScheduleRepository(IMongoDatabase database) : IUnitScheduleRepository
{
    private IMongoCollection<UnitScheduleDocument> Collection =>
        database.GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");

    public async Task<UnitSchedule?> GetByUnitRscAsync(string unitRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitScheduleMapping.ToDomain(doc);
    }

    public async Task<UnitSchedule?> FindByLocationAndTimeAsync(
        string locationCode, DateTime startTime, CancellationToken ct = default)
    {
        var doc = await Collection
            .Find(d => d.LocationCode == locationCode && d.StartTime == startTime)
            .FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitScheduleMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<UnitSchedule>> ListByLocationAndDateAsync(
        string locationCode, DateOnly date, CancellationToken ct = default)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var docs = await Collection
            .Find(d => d.LocationCode == locationCode
                && d.StartTime >= dayStart
                && d.StartTime < dayEnd)
            .SortBy(d => d.StartTime)
            .ToListAsync(ct);
        return docs.Select(UnitScheduleMapping.ToDomain).ToList();
    }

    public async Task AddAsync(UnitSchedule schedule, CancellationToken ct = default)
    {
        var doc = UnitScheduleMapping.ToDocument(schedule);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(UnitSchedule schedule, CancellationToken ct = default)
    {
        var doc = UnitScheduleMapping.ToDocument(schedule);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }

    public async Task DeleteAsync(string unitRsc, CancellationToken ct = default)
    {
        await Collection.DeleteOneAsync(d => d.Id == unitRsc, ct);
    }
}
