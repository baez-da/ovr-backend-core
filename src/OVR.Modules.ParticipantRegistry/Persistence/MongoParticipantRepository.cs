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

    public async Task<IReadOnlyList<Participant>> FindByOrganisationAsync(string organisation, CancellationToken ct = default)
    {
        var docs = await Collection.Find(d => d.Organisation == organisation).ToListAsync(ct);
        return docs.Select(ParticipantMapping.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Participant>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
    {
        var filter = Builders<ParticipantDocument>.Filter.In(d => d.Id, ids);
        var docs = await Collection.Find(filter).ToListAsync(ct);
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

    public async Task<IReadOnlyList<Participant>> FindByOdfDisciplineAsync(
        string discipline, CancellationToken ct = default)
    {
        var filter = Builders<ParticipantDocument>.Filter.And(
            Builders<ParticipantDocument>.Filter.Ne(d => d.Code, null),
            Builders<ParticipantDocument>.Filter.ElemMatch(
                d => d.Functions,
                f => f.Discipline == discipline));

        var docs = await Collection.Find(filter).ToListAsync(ct);
        return docs.Select(ParticipantMapping.ToDomain).ToList();
    }

    public async Task DeleteManyAsync(
        IReadOnlyList<string> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return;
        var filter = Builders<ParticipantDocument>.Filter.In(d => d.Id, ids);
        await Collection.DeleteManyAsync(filter, ct);
    }

    public async Task AddManyAsync(
        IReadOnlyList<Participant> participants, CancellationToken ct = default)
    {
        if (participants.Count == 0) return;
        var docs = participants.Select(ParticipantMapping.ToDocument).ToList();
        await Collection.InsertManyAsync(docs, cancellationToken: ct);
    }
}
