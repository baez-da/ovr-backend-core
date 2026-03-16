using MongoDB.Driver;
using OVR.Modules.ParticipantRegistry.Domain;

namespace OVR.Modules.ParticipantRegistry.Persistence;

internal sealed class MongoParticipantRepository(IMongoDatabase database) : IParticipantRepository
{
    private IMongoCollection<ParticipantDocument> Collection =>
        database.GetCollection<ParticipantDocument>("participantRegistry_participants");

    public async Task<Participant?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == id).FirstOrDefaultAsync(ct);
        return doc is null ? null : ParticipantMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Participant>> FindByNocAsync(string noc, CancellationToken ct = default)
    {
        var docs = await Collection.Find(d => d.Noc == noc).ToListAsync(ct);
        return docs.Select(ParticipantMapping.ToDomain).ToList();
    }

    public async Task AddAsync(Participant participant, CancellationToken ct = default)
    {
        var doc = ParticipantMapping.ToDocument(participant);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Participant participant, CancellationToken ct = default)
    {
        var doc = ParticipantMapping.ToDocument(participant);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }
}
