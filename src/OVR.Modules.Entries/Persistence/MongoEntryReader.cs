using MongoDB.Driver;
using OVR.Modules.Entries.Contracts;
using OVR.Modules.Entries.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Persistence;

public sealed class MongoEntryReader : IEntryReader
{
    private readonly IMongoCollection<EntryDocument> _entries;

    public MongoEntryReader(IMongoDatabase database)
    {
        _entries = database.GetCollection<EntryDocument>("entries_entries");
    }

    public async Task<IReadOnlyList<EntryDto>> ListActiveByEventRsc(
        string eventRsc, CancellationToken ct)
    {
        var activeStatus = EntryStatus.Active.ToString();
        var docs = await _entries
            .Find(e => e.EventRsc == eventRsc && e.Status == activeStatus)
            .ToListAsync(ct);

        return docs.Select(d => new EntryDto(
            ParticipantId.Create(d.ParticipantId),
            int.TryParse(d.Seed, out var n) ? n : (int?)null,
            Organisation.Create(d.Organisation))).ToList();
    }
}
