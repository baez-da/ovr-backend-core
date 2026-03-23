using OVR.Modules.ParticipantRegistry.Domain;

namespace OVR.Modules.ParticipantRegistry.Persistence;

public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> FindByOrganisationAsync(string organisation, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> GetByIdsAsync(IReadOnlyList<string> ids, CancellationToken ct = default);
    Task AddAsync(Participant participant, CancellationToken ct = default);
    Task UpdateAsync(Participant participant, CancellationToken ct = default);
}
