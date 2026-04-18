using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal sealed class MongoEventRepository(IMongoDatabase database) : IEventRepository
{
    private IMongoCollection<EventDocument> Collection =>
        database.GetCollection<EventDocument>("competitionconfig_events");

    public async Task<Event?> GetByRscAsync(string eventRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == eventRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : EventMapping.ToDomain(doc);
    }

    public async Task AddAsync(Event @event, CancellationToken ct = default)
    {
        var doc = EventMapping.ToDocument(@event);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Event @event, CancellationToken ct = default)
    {
        var doc = EventMapping.ToDocument(@event);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }
}
