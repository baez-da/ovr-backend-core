using MongoDB.Driver;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal sealed class MongoSessionRepository(IMongoDatabase database) : ISessionRepository
{
    private IMongoCollection<SessionDocument> Collection =>
        database.GetCollection<SessionDocument>("scheduling_sessions");

    public async Task<Session?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == code).FirstOrDefaultAsync(ct);
        return doc is null ? null : SessionMapping.ToDomain(doc);
    }

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        var doc = SessionMapping.ToDocument(session);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }
}
