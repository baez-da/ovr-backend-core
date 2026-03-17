using OVR.Modules.Entries.Domain;

namespace OVR.Modules.Entries.Persistence;

public interface IEntryRepository
{
    Task<Entry?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Entry>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default);
    Task<IReadOnlyList<Entry>> FindByParticipantIdAsync(string participantId, CancellationToken ct = default);
    Task AddAsync(Entry entry, CancellationToken ct = default);
    Task UpdateAsync(Entry entry, CancellationToken ct = default);
}
